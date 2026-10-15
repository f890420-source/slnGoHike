# GoHike 部署到 Azure Container Apps

此文件說明 Azure Container Apps 環境的建立方式。根目錄的 `Dockerfile` 仍用於建置映像；`.github/workflows/docker.yml` 目前只在 `deploy-sean` 分支的 push 將映像發布到 GHCR，不會自動更新 Container App。PR 只建置與執行 API 測試。第一階段固定一個 replica，`Hangfire` 保持停用。

實際部署未使用 Azure Files。活動照片與會員頭像使用 Cloudinary；以下不需要建立 Azure file share 或設定 volume mount。程式保留自訂 Data Protection 金鑰目錄的支援，但這不代表已配置外部持久化儲存。

## 1. 準備 Azure SQL

先在 Bash 環境安裝 Azure CLI、登入，並確認有建立資源及 private DNS zone 的權限。以下為**首次建立新資源**的範例；執行前確認 `rg-gohike`、`vnet-gohike` 等名稱沒有指向其他現有部署，且 `10.42.0.0/16` 不與要互連的網路重疊。SQL server 名稱使用訂用帳戶 ID 前 12 碼產生候選值；名稱仍須全域唯一，如已被佔用，請改成另一個名稱，並在後面的區塊使用相同值。不要開啟 Bash `set -x`，以免把密碼印到終端紀錄。

```bash
set -e
az login
SUBSCRIPTION_ID='請填入訂用帳戶 ID'
az account set --subscription "$SUBSCRIPTION_ID"

RG='rg-gohike'
LOCATION='eastasia'
VNET='vnet-gohike'
ACA_SUBNET='snet-container-apps'
PE_SUBNET='snet-private-endpoints'
SQL_SERVER="gohikesql$(printf '%s' "$SUBSCRIPTION_ID" | tr -d '-' | cut -c1-12)"
SQL_DB='GoHikeData'
SQL_ADMIN='gohikeadmin'
SQL_PE='pe-gohike-sql'
SQL_DNS_ZONE='privatelink.database.windows.net'

az provider register --namespace Microsoft.App --wait --output none
az group create --name "$RG" --location "$LOCATION" --output none
az network vnet create --resource-group "$RG" --name "$VNET" \
  --location "$LOCATION" --address-prefixes 10.42.0.0/16 --output none
az network vnet subnet create --resource-group "$RG" --vnet-name "$VNET" \
  --name "$ACA_SUBNET" --address-prefixes 10.42.0.0/23 \
  --delegations Microsoft.App/environments --output none
az network vnet subnet create --resource-group "$RG" --vnet-name "$VNET" \
  --name "$PE_SUBNET" --address-prefixes 10.42.2.0/24 --output none

read -r -s -p '請輸入 Azure SQL 管理員密碼：' SQL_ADMIN_PASSWORD
echo
az sql server create --resource-group "$RG" --name "$SQL_SERVER" \
  --location "$LOCATION" --admin-user "$SQL_ADMIN" \
  --admin-password "$SQL_ADMIN_PASSWORD" --enable-public-network true \
  --minimal-tls-version 1.2 --output none
unset SQL_ADMIN_PASSWORD
az sql db create --resource-group "$RG" --server "$SQL_SERVER" \
  --name "$SQL_DB" --service-objective Basic \
  --backup-storage-redundancy Local --output none

SQL_SERVER_ID=$(az sql server show --resource-group "$RG" \
  --name "$SQL_SERVER" --query id --output tsv)
az network private-endpoint create --resource-group "$RG" --name "$SQL_PE" \
  --location "$LOCATION" --vnet-name "$VNET" --subnet "$PE_SUBNET" \
  --private-connection-resource-id "$SQL_SERVER_ID" \
  --group-id sqlServer --connection-name "${SQL_PE}-connection" --output none
az network private-dns zone create --resource-group "$RG" \
  --name "$SQL_DNS_ZONE" --output none
az network private-dns link vnet create --resource-group "$RG" \
  --zone-name "$SQL_DNS_ZONE" --name 'link-gohike-vnet' \
  --virtual-network "$VNET" --registration-enabled false --output none
az network private-endpoint dns-zone-group create --resource-group "$RG" \
  --endpoint-name "$SQL_PE" --name 'sql-zone-group' \
  --private-dns-zone "$SQL_DNS_ZONE" --zone-name 'sql' --output none

ACA_SUBNET_ID=$(az network vnet subnet show --resource-group "$RG" \
  --vnet-name "$VNET" --name "$ACA_SUBNET" --query id --output tsv)
PE_NIC_ID=$(az network private-endpoint show --resource-group "$RG" \
  --name "$SQL_PE" --query 'networkInterfaces[0].id' --output tsv)
PE_IP=$(az network nic show --ids "$PE_NIC_ID" \
  --query 'ipConfigurations[0].privateIPAddress' --output tsv)
printf 'Container Apps 子網路 ID：%s\nSQL 主機名稱：%s.database.windows.net\nPrivate endpoint IP：%s\n' \
  "$ACA_SUBNET_ID" "$SQL_SERVER" "$PE_IP"
```

`Basic` 是先驗證部署的低成本規格，資料庫儲存上限為 **2 GB**；匯入 BACPAC 前先確認來源資料量、匯入所需空間與效能是否適合，不足時先調高資料庫規格。[Azure SQL Basic 資源限制](https://learn.microsoft.com/en-us/azure/azure-sql/database/resource-limits-dtu-single-databases)

### 手動確認：此處先停止

**先不要執行下方關閉公開存取的區塊。** 在同一個 VNet 中可執行命令的 VM，或已整合此 VNet 且具備必要工具的 Container App 內，使用上一段輸出的主機名稱與 IP 驗證：

```bash
SQL_FQDN='<上一段輸出的 SQL 主機名稱>'
nslookup "$SQL_FQDN"       # 結果應包含上一段輸出的 private endpoint IP
nc -vz "$SQL_FQDN" 1433     # 應顯示 TCP 1433 連線成功
sqlcmd -S "tcp:$SQL_FQDN,1433" -d GoHikeData -U gohikeadmin -N -Q 'SELECT 1'
```

`sqlcmd` 會提示輸入密碼；若測試主機沒有 `nc` 或 `sqlcmd`，先安裝工具或使用具備工具的 VNet 內測試主機。名稱解析、TCP 與 SQL 登入三項都成功，才算驗證完成。一般 Azure Cloud Shell 不在此 VNet 內；除非已特別設定其 VNet 隔離，否則不能用它的 `nslookup` 結果代替上述測試。連線字串仍使用 `<server>.database.windows.net`，不要改用私人 IP 或 `privatelink` 網址。[Azure SQL private endpoint](https://learn.microsoft.com/en-us/azure/azure-sql/database/private-endpoint-overview)、[Container Apps VNet 整合](https://learn.microsoft.com/en-us/azure/container-apps/vnet-custom)

**只有手動確認成功後，才執行以下第二段。** 可在新的 Bash session 執行；先選擇與第一段相同的訂用帳戶。此段會再次檢查 private endpoint 已核准、VNet 已連結 private DNS zone，且 DNS A 記錄指向其私人 IP，然後關閉 SQL server 的 public network access：

```bash
set -e
az account set --subscription '請填入與第一段相同的訂用帳戶 ID'
SUBSCRIPTION_ID=$(az account show --query id --output tsv)
RG='rg-gohike'
VNET='vnet-gohike'
SQL_SERVER="gohikesql$(printf '%s' "$SUBSCRIPTION_ID" | tr -d '-' | cut -c1-12)"
SQL_PE='pe-gohike-sql'
SQL_DNS_ZONE='privatelink.database.windows.net'

PE_STATUS=$(az network private-endpoint show --resource-group "$RG" \
  --name "$SQL_PE" \
  --query 'privateLinkServiceConnections[0].privateLinkServiceConnectionState.status' \
  --output tsv)
test "$PE_STATUS" = 'Approved'
DNS_VNET_ID=$(az network private-dns link vnet show --resource-group "$RG" \
  --zone-name "$SQL_DNS_ZONE" --name 'link-gohike-vnet' \
  --query 'virtualNetwork.id' --output tsv)
VNET_ID=$(az network vnet show --resource-group "$RG" --name "$VNET" \
  --query id --output tsv)
test "${DNS_VNET_ID,,}" = "${VNET_ID,,}"
PE_NIC_ID=$(az network private-endpoint show --resource-group "$RG" \
  --name "$SQL_PE" --query 'networkInterfaces[0].id' --output tsv)
PE_IP=$(az network nic show --ids "$PE_NIC_ID" \
  --query 'ipConfigurations[0].privateIPAddress' --output tsv)
DNS_IP=$(az network private-dns record-set a show --resource-group "$RG" \
  --zone-name "$SQL_DNS_ZONE" --name "$SQL_SERVER" \
  --query 'aRecords[0].ipv4Address' --output tsv)
test "$DNS_IP" = "$PE_IP"
az sql server update --resource-group "$RG" --name "$SQL_SERVER" \
  --enable-public-network false --output none
az sql server show --resource-group "$RG" --name "$SQL_SERVER" \
  --query '{server:fullyQualifiedDomainName,publicNetworkAccess:publicNetworkAccess}' \
  --output table
```

最後的 `publicNetworkAccess` 應為 `Disabled`。這只限制連到 SQL 的方向；Container App 仍可使用外部 ingress，讓使用者從網際網路連入應用程式。

1. 暫停來源資料庫寫入，備份並以 `SqlPackage /Action:Export` 匯出包含資料的 BACPAC；以 `/Action:Import` 從可連通 private endpoint 的環境匯入**空** Azure SQL Database。BACPAC 包含資料，請放在受限制的位置，不要加入 Git。先核對來源 SQL Server 使用的功能是否受 Azure SQL Database 支援。[SqlPackage](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage)、[Import](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-import)
2. 比對匯入後的 schema 與 `prjGoHike/DatabaseScripts`。只執行來源資料庫尚未套用的增量腳本；不要以 `*snapshot*.sql` 覆蓋匯入後的資料。確認 `Users`、`Trails` 等主要表與 API 所需的新欄位均存在。專案啟動不會自動套 EF migrations。
3. 建立供應用程式使用的低權限 SQL 帳號，將加密連線字串存成 Container App secret；不要寫入 Dockerfile、GitHub Actions、`appsettings.json` 或 GitHub variables。

## 2. 建立 ACR 與 Container App

以下用 Bash 與 Azure CLI；名稱請先換成實際且全域唯一的值。沿用第一節建立的 VNet 與 Container Apps 子網路。先安裝最新 Azure CLI 與 `containerapp` extension，登入後選定訂用帳戶。

```bash
az login
az account set --subscription '<SUBSCRIPTION_ID>'
az extension add --name containerapp --upgrade
az provider register --namespace Microsoft.OperationalInsights --wait --output none
az provider register --namespace Microsoft.ContainerService --wait --output none

RG='rg-gohike'
LOCATION='eastasia'
ACR='gohikeacrunique'
ENV='cae-gohike'
APP='ca-gohike-api'
VNET='vnet-gohike'
ACA_SUBNET='snet-container-apps'

az group create --name "$RG" --location "$LOCATION"
az acr create --name "$ACR" --resource-group "$RG" --location "$LOCATION" \
  --sku Basic --role-assignment-mode rbac-abac
ACA_SUBNET_ID=$(az network vnet subnet show --resource-group "$RG" \
  --vnet-name "$VNET" --name "$ACA_SUBNET" --query id --output tsv)
az containerapp env create --name "$ENV" --resource-group "$RG" --location "$LOCATION" \
  --infrastructure-subnet-resource-id "$ACA_SUBNET_ID"
az containerapp create --name "$APP" --resource-group "$RG" --environment "$ENV" \
  --image mcr.microsoft.com/dotnet/samples:aspnetapp --ingress external --target-port 8080 \
  --min-replicas 1 --max-replicas 1
az containerapp revision set-mode --name "$APP" --resource-group "$RG" --mode single
az containerapp identity assign --name "$APP" --resource-group "$RG" --system-assigned
ACR_ID=$(az acr show --name "$ACR" --resource-group "$RG" --query id --output tsv)
APP_PRINCIPAL=$(az containerapp show --name "$APP" --resource-group "$RG" --query identity.principalId --output tsv)
az role assignment create --assignee-object-id "$APP_PRINCIPAL" \
  --assignee-principal-type ServicePrincipal \
  --role 'Container Registry Repository Reader' --scope "$ACR_ID"
az containerapp registry set --name "$APP" --resource-group "$RG" \
  --server "$ACR.azurecr.io" --identity system
```

上述公開範例映像同樣監聽 `8080`，供 Container App 尚未部署 GoHike 時啟動。此處新建的 ACR 使用 RBAC + ABAC 模式，因此給 Container App 身分 `Container Registry Repository Reader`；若改用現有的 RBAC-only ACR，對應角色是 `AcrPull`。授權傳播可能需數分鐘。若日後改用 ACR 私有映像，須先確認此身分已能拉取映像。[範例映像與埠號](https://learn.microsoft.com/en-us/azure/container-apps/tutorial-scaling)、[受控身分拉取映像](https://learn.microsoft.com/en-us/azure/container-apps/managed-identity-image-pull)、[ACR 權限模式](https://learn.microsoft.com/en-us/azure/container-registry/container-registry-rbac-abac-repository-permissions)

## 3. 圖片儲存與 Data Protection 金鑰

活動照片與會員頭像由後端透過 `CloudinaryService` 上傳至 Cloudinary，資料庫保存回傳的 HTTPS URL，前端直接使用該網址顯示圖片。圖片儲存不需要 Azure Files。請將 `Cloudinary__CloudName`、`Cloudinary__ApiKey`、`Cloudinary__ApiSecret` 設定於 Container App，並將憑證連結到 secrets。完整流程見[活動圖片串接文件](EventPhotos_api.md)。

既有 `/assets/JoinGroup_Images/` 圖片仍保留相容處理，但程式保留舊路徑不代表舊檔案已搬到外部儲存。若資料庫仍有這類網址，須另外確認對應檔案在部署後能否讀取。

Data Protection 用於保護重設密碼、信箱驗證等資料。`Program.cs` 支援以 `DataProtection__KeysPath` 指定金鑰目錄；Dockerfile 也建立了 `/app/dpkeys`，但建立容器內目錄不等於配置持久化儲存。本次部署未使用 Azure Files，也未確認是否另有金鑰持久化設定，因此不能宣稱金鑰已跨 revision 保存，或先前產生的驗證連結在重新部署後一定有效。`DataProtection__KeysPath` 為選用設定，不列為目前部署的必要環境變數。

## 4. 設定 Container App

在 Azure Portal 的 Container App > Secrets 建立 `sql-connection` 與 `jwt-secret-key`；再到 Containers > Environment variables 設定下表。`secretref:` 指向 Container App secret。更新環境變數後建立新 revision。[環境變數與 secret reference](https://learn.microsoft.com/en-us/azure/container-apps/environment-variables)

| 環境變數 | 值 |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` |
| `ConnectionStrings__GoHikeDataContext` | `secretref:sql-connection` |
| `JwtSettings__SecretKey` | `secretref:jwt-secret-key`，UTF-8 至少 32 bytes |
| `JwtSettings__Issuer`, `JwtSettings__Audience` | 實際正式值，並與 client 的 JWT 驗證設定一致 |
| `Hangfire__Enabled` | `false` |
| `Cors__AllowedOrigins__0` | 前端網址確定後才填入其完整 origin，例如 `https://frontend.example.com`；不填時正式環境不允許跨來源瀏覽器請求 |

如需註冊、寄信、圖片或 Gemini 功能，也要按實際使用情形設定 `Email__*`、`PasswordReset__*`、`GoogleAuth__ClientId`、`Cloudinary__*`、`Gemini__ApiKey`。檢查這些值需要哪些憑證，放進 Container App secrets；不要假設本機 User Secrets 會進入映像。`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` 讓 ASP.NET Core 讀取 Azure ingress 傳來的 HTTPS scheme，避免錯誤重新導向和產生 `http://` 照片 URL。[ASP.NET Core 反向代理](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)

在 Container App 的 Probes 設 HTTP liveness `GET /health/live`、HTTP readiness `GET /health/ready`，port 均為 `8080`。Readiness 會檢查 Azure SQL 連線；SQL 暫時不可用時回 `503`。Startup probe 可使用 `/health/live`，並給予足夠的啟動等待時間。[Container Apps health probes](https://learn.microsoft.com/en-us/azure/container-apps/health-probes)

## 5. GitHub Actions 發布映像到 GHCR

`deploy-sean` 分支的 push 通過 API 建置與測試後，workflow 使用 `GITHUB_TOKEN` 推送映像到 `ghcr.io/<owner>/<repository>:<GITHUB_SHA>`，其中 owner 與 repository 會轉為小寫。GitHub Actions 的 token 需要 `packages: write` 權限；workflow 已設定，不需要 Azure OIDC 或 ACR 的 Actions variables。PR 只建置容器，不登入 GHCR，也不推送映像。[GitHub 容器映像發布說明](https://docs.github.com/en/actions/tutorials/publish-packages/publish-docker-images)

GHCR 是映像倉庫，不會執行容器。上文建立的 Container App 仍設定為從 ACR 拉取映像；如果要改成執行 GHCR 映像，須先讓 Container App 取得該 GHCR package 的讀取權限並變更其 registry 設定，再更新 image。私有 package 需要具備 `read:packages` 權限的憑證；公開 package 則可匿名拉取。workflow 目前不會執行這些 Azure 更新。[GHCR 存取權限](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)

## 6. 驗收與綁定網域

取得配發的網址：

```bash
az containerapp show --name "$APP" --resource-group "$RG" \
  --query 'properties.configuration.ingress.fqdn' --output tsv
az containerapp revision list --name "$APP" --resource-group "$RG" --output table
az containerapp logs show --name "$APP" --resource-group "$RG" --follow
```

手動更新 Container App 映像後，先檢查 `/health/live` 與 `/health/ready` 均為 `200`，再驗證 SQL 讀寫、JWT 登入、Cloudinary 照片上傳與讀取、密碼重設／信箱驗證，以及 SignalR 連線。確認資料庫保存的是 Cloudinary 圖片網址，且更新 revision 後仍可讀取圖片。另測試更新前產生的密碼重設／信箱驗證連結在更新後是否仍有效，記錄實際結果；目前未確認金鑰持久化，不應預先保證跨 revision 有效。確認 revision 使用的是預期的 SHA tag。若失敗，從 Container Apps revisions 回復先前可用的映像；資料庫 schema 變更須另外規劃回復方式。[Revisions](https://learn.microsoft.com/en-us/azure/container-apps/revisions-manage)

最後在 DNS 設 `gohike-cloud.clam.tw` 的 CNAME 指向上述配發 FQDN，並以 Container App 的 `customDomainVerificationId` 建立 `asuid.gohike-cloud.clam.tw` TXT。使用 `az containerapp hostname add` 與 `az containerapp hostname bind --validation-method CNAME` 綁定免費 managed certificate。若 DNS 服務有代理功能，憑證簽發期間須讓 CNAME **直接**指向 Container App。待正式前端網址確定，再填 `Cors__AllowedOrigins__0` 並驗證瀏覽器請求。[自訂網域與受控憑證](https://learn.microsoft.com/en-us/azure/container-apps/custom-domains-managed-certificates)

目前固定一個 replica，因為 MVC Session 與 SignalR 連線狀態仍在容器記憶體。擴為多副本前，先將 Session 改用共享儲存，並為 SignalR 加入跨副本服務。
