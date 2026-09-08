using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using prjGoHike.Models;
using prjGoHike.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace prjGoHike.Controllers
{
    public class PersonalEquipmentController : Controller
    {
        private readonly GoHikeDataContext _db;

        // TODO：登入功能整合後，改由 Claim 取得會員 ID
        private const long DevelopmentMemberId = 2;

        public PersonalEquipmentController(GoHikeDataContext db)
        {
            _db = db;
        }

        private long? GetCurrentUserId()
        {
            string? userIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (long.TryParse(
                userIdClaim,
                out long userId))
            {
                return userId;
            }

            return null;
        }

        // STEP 05：顯示裝備清單記錄
        public IActionResult Index(
            string? searchKeyword,
            string? statusFilter)
        {
            IQueryable<PersonalEquipmentList> query =
                _db.PersonalEquipmentLists
                    .AsNoTracking()
                    .Where(list =>
                        !list.IsDeleted);

            string normalizedKeyword =
                searchKeyword?.Trim() ?? "";

            string normalizedStatus =
                statusFilter?.Trim() ?? "";

            if (!string.IsNullOrEmpty(
                normalizedKeyword))
            {
                query = query.Where(list =>
                    list.ListName.Contains(
                        normalizedKeyword)
                    || list.Mountain.MountainName.Contains(
                        normalizedKeyword));
            }

            string[] allowedStatuses =
            {
        "安全範圍",
        "接近安全上限",
        "超過安全上限"
    };

            if (allowedStatuses.Contains(
                normalizedStatus))
            {
                query = query.Where(list =>
                    list.WeightStatus ==
                    normalizedStatus);
            }

            List<CPersonalEquipmentListItemViewModel>
                items =
                    query
                        .OrderByDescending(list =>
                            list.CreatedAt)
                        .Select(list =>
                            new
                            CPersonalEquipmentListItemViewModel
                            {
                                ListId =
                                    list.ListId,

                                ListName =
                                    list.ListName,

                                MountainName =
                                    list.Mountain.MountainName,

                                HikingDate =
                                    list.HikingDate,

                                HikingDays =
                                    list.HikingDays,

                                TotalWeightGram =
                                    list.TotalWeightGram,

                                WeightStatus =
                                    list.WeightStatus,

                                CreatedAt =
                                    list.CreatedAt
                            })
                        .ToList();

            CPersonalEquipmentListIndexViewModel vm =
                new CPersonalEquipmentListIndexViewModel
                {
                    SearchKeyword =
                        normalizedKeyword,

                    StatusFilter =
                        normalizedStatus,

                    Items =
                        items
                };

            return View(vm);
        }

        public IActionResult TestDatabase()
        {
            int mountainCount = _db.Mountains.Count();

            return Content(
                $"資料庫連線成功，目前共有 {mountainCount} 筆山岳資料。");
        }
        // GET：顯示表單
        public IActionResult Create()
        {

            HttpContext.Session.Remove(
            CDictionary.SK_PERSONAL_EQUIPMENT_EDIT_LIST_ID);

            var mountains = _db.Mountains
                .OrderBy(m => m.MountainName)
                .ToList();

            ViewBag.Mountains = new SelectList(
                mountains,
                "MountainId",
                "MountainName");

            CEquipmentConditionViewModel vm =
                new CEquipmentConditionViewModel();

            return View(vm);
        }
        // POST：接收表單
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create( CEquipmentConditionViewModel vm)
        {
            bool mountainExists = _db.Mountains
                .Any(m => m.MountainId == vm.MountainId);

            if (!mountainExists)
            {
                ModelState.AddModelError(
                    "MountainId",
                    "選擇的山岳不存在");
            }

            if (vm.HikingDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    "HikingDate",
                    "登山日期不可早於今天");
            }

            if (!ModelState.IsValid)
            {
                var mountains = _db.Mountains
                    .OrderBy(m => m.MountainName)
                    .ToList();

                ViewBag.Mountains = new SelectList(
                    mountains,
                    "MountainId",
                    "MountainName",
                    vm.MountainId);

                return View(vm);
            }

            string json =
                JsonSerializer.Serialize(vm);

            HttpContext.Session.SetString(
                CDictionary.SK_PERSONAL_EQUIPMENT_CONDITION,
                json);

            return RedirectToAction("Suggestion");
        }

        // GET：Suggestion
        public IActionResult Suggestion()
        {
            string? json = HttpContext.Session.GetString(
                CDictionary.SK_PERSONAL_EQUIPMENT_CONDITION);

            if (string.IsNullOrEmpty(json))
            {
                return RedirectToAction("Create");
            }

            CEquipmentConditionViewModel? condition =
                JsonSerializer.Deserialize
                <CEquipmentConditionViewModel>(json);

            if (condition == null)
            {
                return RedirectToAction("Create");
            }

            Mountain? mountain = _db.Mountains
                .FirstOrDefault(
                    m => m.MountainId == condition.MountainId);

            if (mountain == null)
            {
                return RedirectToAction("Create");
            }

            List<CEquipmentSuggestionItemViewModel> items =
                _db.MountainEquipmentSuggestions
                    .Where(s =>
                        s.MountainId == condition.MountainId

                        && (s.Season == condition.Season
                            || s.Season == "全年")

                        && s.MinimumDays <= condition.HikingDays

                        && (s.MaximumDays == null
                            || s.MaximumDays >= condition.HikingDays)

                        && (s.IntensityLevel == null
                            || s.IntensityLevel ==
                               condition.IntensityLevel)

                        && (s.ExperienceLevel == null
                            || s.ExperienceLevel ==
                               condition.ExperienceLevel)

                        && s.Equipment.IsActive

                        && s.Equipment.Category.IsActive)

                    .OrderBy(s =>
                        s.Equipment.Category.SortOrder)

                    .ThenBy(s =>
                        s.Equipment.EquipmentName)

                    .Select(s =>
                        new CEquipmentSuggestionItemViewModel
                        {
                            EquipmentId =
                                s.EquipmentId,

                            CategoryName =
                                s.Equipment.Category.CategoryName,

                            EquipmentName =
                                s.Equipment.EquipmentName,

                            Quantity =
                                s.SuggestedQuantity,

                            UnitWeightGram =
                                s.Equipment.StandardWeightGram,

                            RequirementLevel =
                                s.RequirementLevel,

                            Notes =
                                s.Notes,

                            IsSelected =
                                s.RequirementLevel == "必備"
                        })

                    .ToList();

            CEquipmentSuggestionViewModel vm =
                new CEquipmentSuggestionViewModel
                {
                    Condition = condition,
                    MountainName = mountain.MountainName,
                    Items = items
                };

            return View(vm);
        }

        // POST：Suggestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Suggestion(CEquipmentSuggestionViewModel vm,string submitMode)
        {
            string? conditionJson =
                HttpContext.Session.GetString(
                    CDictionary.SK_PERSONAL_EQUIPMENT_CONDITION);

            if (string.IsNullOrEmpty(conditionJson))
            {
                return RedirectToAction("Create");
            }

            CEquipmentConditionViewModel? condition =
                JsonSerializer.Deserialize
                <CEquipmentConditionViewModel>(conditionJson);

            if (condition == null)
            {
                return RedirectToAction("Create");
            }

            List<long> selectedEquipmentIds;

            if (submitMode == "all")
            {
                selectedEquipmentIds = vm.Items
                    .Select(item => item.EquipmentId)
                    .Distinct()
                    .ToList();
            }
            else
            {
                selectedEquipmentIds = vm.Items
                    .Where(item => item.IsSelected)
                    .Select(item => item.EquipmentId)
                    .Distinct()
                    .ToList();
            }

            if (selectedEquipmentIds.Count == 0)
            {
                TempData["ErrorMessage"] =
                    "請至少勾選一件裝備";

                return RedirectToAction("Suggestion");
            }

            List<CEquipmentSuggestionItemViewModel> selectedItems =
                _db.MountainEquipmentSuggestions
                    .Where(s =>
                        selectedEquipmentIds.Contains(
                            s.EquipmentId)

                        && s.MountainId ==
                           condition.MountainId

                        && (s.Season == condition.Season
                            || s.Season == "全年")

                        && s.MinimumDays <=
                           condition.HikingDays

                        && (s.MaximumDays == null
                            || s.MaximumDays >=
                               condition.HikingDays)

                        && (s.IntensityLevel == null
                            || s.IntensityLevel ==
                               condition.IntensityLevel)

                        && (s.ExperienceLevel == null
                            || s.ExperienceLevel ==
                               condition.ExperienceLevel)

                        && s.Equipment.IsActive

                        && s.Equipment.Category.IsActive)

                    .OrderBy(s =>
                        s.Equipment.Category.SortOrder)

                    .ThenBy(s =>
                        s.Equipment.EquipmentName)

                    .Select(s =>
                        new CEquipmentSuggestionItemViewModel
                        {
                            EquipmentId =
                                s.EquipmentId,

                            CategoryName =
                                s.Equipment.Category.CategoryName,

                            EquipmentName =
                                s.Equipment.EquipmentName,

                            Quantity =
                                s.SuggestedQuantity,

                            UnitWeightGram =
                                s.Equipment.StandardWeightGram,

                            RequirementLevel =
                                s.RequirementLevel,

                            Notes =
                                s.Notes,

                            IsSelected = true
                        })

                    .ToList();

            selectedItems = selectedItems
                .GroupBy(item => item.EquipmentId)
                .Select(group => group.First())
                .ToList();

            if (selectedItems.Count == 0)
            {
                TempData["ErrorMessage"] =
                    "找不到符合條件的有效裝備";

                return RedirectToAction("Suggestion");
            }

            string itemsJson =
                JsonSerializer.Serialize(selectedItems);

            HttpContext.Session.SetString(
                CDictionary.SK_PERSONAL_EQUIPMENT_ITEMS,
                itemsJson);

            return RedirectToAction("EditItems");
        }

        //STEP 03：編輯我的裝備清單
        public IActionResult EditItems()
        {
            string? conditionJson =
                HttpContext.Session.GetString(
                    CDictionary.SK_PERSONAL_EQUIPMENT_CONDITION);

            string? itemsJson =
                HttpContext.Session.GetString(
                    CDictionary.SK_PERSONAL_EQUIPMENT_ITEMS);

            if (string.IsNullOrEmpty(conditionJson)
                || string.IsNullOrEmpty(itemsJson))
            {
                return RedirectToAction("Suggestion");
            }

            CEquipmentConditionViewModel? condition =
                JsonSerializer.Deserialize
                <CEquipmentConditionViewModel>(
                    conditionJson);

            List<CPersonalEquipmentItemViewModel>? items =
                JsonSerializer.Deserialize
                <List<CPersonalEquipmentItemViewModel>>(
                    itemsJson);

            if (condition == null
                || items == null
                || items.Count == 0)
            {
                return RedirectToAction("Suggestion");
            }

            Mountain? mountain = _db.Mountains
                .FirstOrDefault(
                    m => m.MountainId ==
                         condition.MountainId);

            if (mountain == null)
            {
                return RedirectToAction("Create");
            }

            CEquipmentEditViewModel vm =
                new CEquipmentEditViewModel
                {
                    Condition = condition,
                    MountainName = mountain.MountainName,
                    Items = items
                };

            string normalizedItemsJson =
                JsonSerializer.Serialize(vm.Items);

            HttpContext.Session.SetString(
                CDictionary.SK_PERSONAL_EQUIPMENT_ITEMS,
                normalizedItemsJson);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditItems(CEquipmentEditViewModel vm,int? deleteIndex,string? submitMode)
        {
            string? itemsJson =
                HttpContext.Session.GetString(
                    CDictionary.SK_PERSONAL_EQUIPMENT_ITEMS);

            if (string.IsNullOrEmpty(itemsJson))
            {
                return RedirectToAction("Suggestion");
            }

            List<CPersonalEquipmentItemViewModel>? sessionItems =
                JsonSerializer.Deserialize
                <List<CPersonalEquipmentItemViewModel>>(
                    itemsJson);

            if (sessionItems == null
                || sessionItems.Count == 0)
            {
                return RedirectToAction("Suggestion");
            }

            // 防止使用者竄改表單，數量必須與 Session 相同
            if (vm.Items.Count != sessionItems.Count)
            {
                TempData["ErrorMessage"] =
                    "裝備資料不一致，請重新操作。";

                return RedirectToAction("EditItems");
            }

            // 更新允許使用者修改的欄位
            for (int i = 0;
                 i < sessionItems.Count;
                 i++)
            {
                int quantity =
                    vm.Items[i].Quantity;

                int unitWeightGram =
                    vm.Items[i].UnitWeightGram;

                if (quantity < 1 || quantity > 20)
                {
                    TempData["ErrorMessage"] =
                        "裝備數量必須介於 1 至 20。";

                    return RedirectToAction("EditItems");
                }

                if (unitWeightGram < 0
                    || unitWeightGram > 50000)
                {
                    TempData["ErrorMessage"] =
                        "單件重量必須介於 0 至 50000 公克。";

                    return RedirectToAction("EditItems");
                }

                sessionItems[i].Quantity =
                    quantity;

                sessionItems[i].UnitWeightGram =
                    unitWeightGram;
            }

            // 新增使用者自訂裝備
            if (submitMode == "addCustom")
            {
                string customName =
                    vm.NewCustomEquipmentName.Trim();

                if (string.IsNullOrEmpty(customName))
                {
                    TempData["ErrorMessage"] =
                        "請輸入自訂裝備名稱。";

                    return RedirectToAction("EditItems");
                }

                if (customName.Length > 100)
                {
                    TempData["ErrorMessage"] =
                        "自訂裝備名稱不可超過 100 個字。";

                    return RedirectToAction("EditItems");
                }

                if (vm.NewCustomQuantity < 1
                    || vm.NewCustomQuantity > 20)
                {
                    TempData["ErrorMessage"] =
                        "自訂裝備數量必須介於 1 至 20。";

                    return RedirectToAction("EditItems");
                }

                if (vm.NewCustomUnitWeightGram < 0
                    || vm.NewCustomUnitWeightGram > 50000)
                {
                    TempData["ErrorMessage"] =
                        "自訂裝備重量必須介於 0 至 50000 公克。";

                    return RedirectToAction("EditItems");
                }

                CPersonalEquipmentItemViewModel customItem =
                    new CPersonalEquipmentItemViewModel
                    {
                        EquipmentId = null,

                        EquipmentName =
                            customName,

                        CategoryName =
                            "自訂裝備",

                        Quantity =
                            vm.NewCustomQuantity,

                        UnitWeightGram =
                            vm.NewCustomUnitWeightGram,

                        RequirementLevel =
                            "自訂",

                        Notes =
                            "使用者新增的自訂裝備"
                    };

                sessionItems.Add(customItem);

                string customItemsJson =
                    JsonSerializer.Serialize(
                        sessionItems);

                HttpContext.Session.SetString(
                    CDictionary.SK_PERSONAL_EQUIPMENT_ITEMS,
                    customItemsJson);

                TempData["SuccessMessage"] =
                    "自訂裝備已新增。";

                return RedirectToAction("EditItems");
            }

            // 有傳入刪除位置時，刪除該筆裝備
            if (deleteIndex.HasValue)
            {
                int index =
                    deleteIndex.Value;

                if (index < 0
                    || index >= sessionItems.Count)
                {
                    TempData["ErrorMessage"] =
                        "找不到要刪除的裝備。";

                    return RedirectToAction("EditItems");
                }

                if (sessionItems.Count == 1)
                {
                    TempData["ErrorMessage"] =
                        "裝備清單至少必須保留一筆資料。";

                    return RedirectToAction("EditItems");
                }

                sessionItems.RemoveAt(index);

                TempData["SuccessMessage"] =
                    "裝備已刪除。";
            }
            else if (submitMode != "analysis")
            {
                TempData["SuccessMessage"] =
                    "裝備修改已儲存。";
            }

            string updatedItemsJson =
                JsonSerializer.Serialize(
                    sessionItems);

            HttpContext.Session.SetString(
                CDictionary.SK_PERSONAL_EQUIPMENT_ITEMS,
                updatedItemsJson);

            if (submitMode == "analysis")
            {
                return RedirectToAction(
                    "WeightAnalysis");
            }

            return RedirectToAction("EditItems");
        }

        // STEP 04：計算並分析裝備重量
        public IActionResult WeightAnalysis()
        {
            string? conditionJson =
                HttpContext.Session.GetString(
                    CDictionary.SK_PERSONAL_EQUIPMENT_CONDITION);

            string? itemsJson =
                HttpContext.Session.GetString(
                    CDictionary.SK_PERSONAL_EQUIPMENT_ITEMS);

            if (string.IsNullOrEmpty(conditionJson)
                || string.IsNullOrEmpty(itemsJson))
            {
                return RedirectToAction("Create");
            }

            CEquipmentConditionViewModel? condition =
                JsonSerializer.Deserialize
                <CEquipmentConditionViewModel>(
                    conditionJson);

            List<CPersonalEquipmentItemViewModel>? items =
                JsonSerializer.Deserialize
                <List<CPersonalEquipmentItemViewModel>>(
                    itemsJson);

            if (condition == null
                || items == null
                || items.Count == 0)
            {
                return RedirectToAction("EditItems");
            }

            Mountain? mountain = _db.Mountains
                .FirstOrDefault(m =>
                    m.MountainId ==
                    condition.MountainId);

            if (mountain == null)
            {
                return RedirectToAction("Create");
            }

            CEquipmentWeightAnalysisViewModel vm =
                new CEquipmentWeightAnalysisViewModel
                {
                    Condition = condition,
                    MountainName = mountain.MountainName,
                    Items = items
                };

            return View(vm);
        }

        // STEP 05：儲存個人裝備清單
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult SaveList()
        //{
        //    //long? memberId =
        //    //    GetCurrentUserId();

        //    //if (!memberId.HasValue)
        //    //{
        //    //    TempData["ErrorMessage"] =
        //    //        "請先登入後再儲存裝備清單。";

        //    //    return RedirectToAction(
        //    //        "WeightAnalysis");
        //    //}

        //    long memberId = DevelopmentMemberId;

        //    bool memberExists =
        //        _db.Users.Any(u =>
        //            u.UserId == memberId);

        //    if (!memberExists)
        //    {
        //        TempData["ErrorMessage"] =
        //            "找不到開發用測試會員，請確認會員資料。";

        //        return RedirectToAction(
        //            "WeightAnalysis");
        //    }

        //    string? conditionJson =
        //        HttpContext.Session.GetString(
        //            CDictionary.SK_PERSONAL_EQUIPMENT_CONDITION);

        //    string? itemsJson =
        //        HttpContext.Session.GetString(
        //            CDictionary.SK_PERSONAL_EQUIPMENT_ITEMS);

        //    if (string.IsNullOrEmpty(conditionJson)
        //        || string.IsNullOrEmpty(itemsJson))
        //    {
        //        TempData["ErrorMessage"] =
        //            "清單資料已失效，請重新建立。";

        //        return RedirectToAction("Create");
        //    }

        //    CEquipmentConditionViewModel? condition =
        //        JsonSerializer.Deserialize
        //        <CEquipmentConditionViewModel>(
        //            conditionJson);

        //    List<CPersonalEquipmentItemViewModel>? items =
        //        JsonSerializer.Deserialize
        //        <List<CPersonalEquipmentItemViewModel>>(
        //            itemsJson);

        //    if (condition == null
        //        || items == null
        //        || items.Count == 0)
        //    {
        //        TempData["ErrorMessage"] =
        //            "清單資料不完整，請重新操作。";

        //        return RedirectToAction("Create");
        //    }

        //    return Content(
        //        $"儲存前檢查成功。" +
        //        //$"會員編號：{memberId.Value}，" +
        //        $"會員編號：{memberId}，" +
        //        $"清單名稱：{condition.ListName}，" +
        //        $"裝備項目：{items.Count} 筆。");
        //}

        // STEP 05：儲存個人裝備清單
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveList()
        {
            long memberId = DevelopmentMemberId;

            bool memberExists =
                _db.Users.Any(u =>
                    u.UserId == memberId);

            if (!memberExists)
            {
                TempData["ErrorMessage"] =
                    "找不到開發用測試會員，請確認會員資料。";

                return RedirectToAction(
                    "WeightAnalysis");
            }

            string? conditionJson =
                HttpContext.Session.GetString(
                    CDictionary.SK_PERSONAL_EQUIPMENT_CONDITION);

            string? itemsJson =
                HttpContext.Session.GetString(
                    CDictionary.SK_PERSONAL_EQUIPMENT_ITEMS);

            if (string.IsNullOrEmpty(conditionJson)
                || string.IsNullOrEmpty(itemsJson))
            {
                TempData["ErrorMessage"] =
                    "清單資料已失效，請重新建立。";

                return RedirectToAction("Create");
            }

            CEquipmentConditionViewModel? condition =
                JsonSerializer.Deserialize
                <CEquipmentConditionViewModel>(
                    conditionJson);

            List<CPersonalEquipmentItemViewModel>? items =
                JsonSerializer.Deserialize
                <List<CPersonalEquipmentItemViewModel>>(
                    itemsJson);

            if (condition == null
                || items == null
                || items.Count == 0)
            {
                TempData["ErrorMessage"] =
                    "清單資料不完整，請重新操作。";

                return RedirectToAction("Create");
            }

            Mountain? mountain =
                _db.Mountains.FirstOrDefault(m =>
                    m.MountainId ==
                    condition.MountainId);

            if (mountain == null)
            {
                TempData["ErrorMessage"] =
                    "指定的山岳不存在。";

                return RedirectToAction("Create");
            }

            // 驗證 Session 中的正式裝備仍存在且啟用
            List<long> equipmentIds =
                items
                    .Where(item =>
                        item.EquipmentId.HasValue)
                    .Select(item =>
                        item.EquipmentId!.Value)
                    .Distinct()
                    .ToList();

            int validEquipmentCount =
                _db.Equipments.Count(e =>
                    equipmentIds.Contains(
                        e.EquipmentId)
                    && e.IsActive);

            if (validEquipmentCount !=
                equipmentIds.Count)
            {
                TempData["ErrorMessage"] =
                    "部分裝備資料已不存在或停用，請重新建立清單。";

                return RedirectToAction("Create");
            }

            // 再次驗證數量與重量，避免 Session 異常
            bool hasInvalidItem =
                items.Any(item =>
                    item.Quantity < 1
                    || item.Quantity > 20
                    || item.UnitWeightGram < 0
                    || item.UnitWeightGram > 50000
                    || string.IsNullOrWhiteSpace(
                        item.EquipmentName));

            if (hasInvalidItem)
            {
                TempData["ErrorMessage"] =
                    "裝備資料格式不正確，請重新調整清單。";

                return RedirectToAction("EditItems");
            }

            CEquipmentWeightAnalysisViewModel analysis =
                new CEquipmentWeightAnalysisViewModel
                {
                    Condition = condition,
                    MountainName =
                        mountain.MountainName,
                    Items = items
                };

            int maxCarryWeightGram =
                decimal.ToInt32(
                    analysis.MaxCarryWeightKg *
                    1000M);

            int remainingWeightGram =
                decimal.ToInt32(
                    analysis.RemainingWeightKg *
                    1000M);

            string? editListIdText =
    HttpContext.Session.GetString(
        CDictionary.SK_PERSONAL_EQUIPMENT_EDIT_LIST_ID);

            bool isEditMode =
                long.TryParse(
                    editListIdText,
                    out long editListId);

            using var transaction =
                _db.Database.BeginTransaction();

            try
            {
                PersonalEquipmentList list;

                if (isEditMode)
                {
                    PersonalEquipmentList? existingList =
                        _db.PersonalEquipmentLists
                            .Include(item =>
                                item.PersonalEquipmentDetails)
                            .FirstOrDefault(item =>
                                item.ListId == editListId
                                && item.MemberId == memberId
                                && !item.IsDeleted);

                    if (existingList == null)
                    {
                        transaction.Rollback();

                        TempData["ErrorMessage"] =
                            "找不到要編輯的裝備清單。";

                        return RedirectToAction("Index");
                    }

                    list = existingList;

                    List<PersonalEquipmentDetail>
                        oldDetails =
                            list.PersonalEquipmentDetails
                                .ToList();

                    _db.PersonalEquipmentDetails
                        .RemoveRange(oldDetails);

                    list.UpdatedAt = DateTime.Now;
                }
                else
                {
                    list = new PersonalEquipmentList
                    {
                        MemberId = memberId,
                        IsDeleted = false,
                        CreatedAt = DateTime.Now
                    };

                    _db.PersonalEquipmentLists.Add(
                        list);
                }

                // 新增與編輯都要更新的清單主檔欄位
                list.MountainId =
                    condition.MountainId;

                list.ListName =
                    condition.ListName.Trim();

                list.HikingDate =
                    DateOnly.FromDateTime(
                        condition.HikingDate);

                list.HikingDays =
                    condition.HikingDays;

                list.Season =
                    condition.Season;

                list.IntensityLevel =
                    condition.IntensityLevel;

                list.ExperienceLevel =
                    condition.ExperienceLevel;

                list.BodyWeightKg =
                    condition.BodyWeightKg;

                list.MaxCarryWeightGram =
                    maxCarryWeightGram;

                list.TotalWeightGram =
                    analysis.TotalWeightGram;

                list.RemainingWeightGram =
                    remainingWeightGram;

                list.WeightPercentage =
                    analysis.BodyWeightPercentage;

                list.WeightStatus =
                    analysis.WeightStatus;

                // 重新建立這份清單的裝備明細
                for (int index = 0;
                     index < items.Count;
                     index++)
                {
                    CPersonalEquipmentItemViewModel item =
                        items[index];

                    PersonalEquipmentDetail detail =
                        new PersonalEquipmentDetail
                        {
                            EquipmentId =
                                item.EquipmentId,

                            CustomEquipmentName =
                                item.EquipmentId.HasValue
                                    ? null
                                    : item.EquipmentName.Trim(),

                            Quantity =
                                item.Quantity,

                            UnitWeightGram =
                                item.UnitWeightGram,

                            TotalWeightGram =
                                item.TotalWeightGram,

                            RequirementLevel =
                                item.RequirementLevel,

                            IsPrepared = false,

                            SortOrder =
                                index + 1,

                            Notes =
                                item.Notes
                        };

                    list.PersonalEquipmentDetails.Add(
                        detail);
                }

                _db.SaveChanges();

                transaction.Commit();

                HttpContext.Session.Remove(
                    CDictionary.SK_PERSONAL_EQUIPMENT_CONDITION);

                HttpContext.Session.Remove(
                    CDictionary.SK_PERSONAL_EQUIPMENT_ITEMS);

                HttpContext.Session.Remove(
                    CDictionary.SK_PERSONAL_EQUIPMENT_EDIT_LIST_ID);

                TempData["SuccessMessage"] =
                    isEditMode
                        ? $"裝備清單「{list.ListName}」已成功更新。"
                        : $"裝備清單「{list.ListName}」已成功儲存。";

                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                transaction.Rollback();

                TempData["ErrorMessage"] =
                    "清單已被其他操作更新，請重新載入後再試。";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                TempData["ErrorMessage"] =
                    "儲存失敗：" +
                    ex.GetBaseException().Message;

                return RedirectToAction(
                    "WeightAnalysis");
            }
        }

        // STEP 05：查看裝備清單詳細內容
        public IActionResult Details(long id)
        {
            CPersonalEquipmentListDetailsViewModel? vm =
                _db.PersonalEquipmentLists
                    .AsNoTracking()
                    .Where(list =>
                        list.ListId == id
                        && !list.IsDeleted)
                    .Select(list =>
                        new
                        CPersonalEquipmentListDetailsViewModel
                        {
                            ListId =
                                list.ListId,

                            ListName =
                                list.ListName,

                            MountainName =
                                list.Mountain.MountainName,

                            HikingDate =
                                list.HikingDate,

                            HikingDays =
                                list.HikingDays,

                            Season =
                                list.Season,

                            IntensityLevel =
                                list.IntensityLevel ?? "",

                            ExperienceLevel =
                                list.ExperienceLevel ?? "",

                            BodyWeightKg =
                                list.BodyWeightKg,

                            MaxCarryWeightGram =
                                list.MaxCarryWeightGram,

                            TotalWeightGram =
                                list.TotalWeightGram,

                            RemainingWeightGram =
                                list.RemainingWeightGram,

                            WeightPercentage =
                                list.WeightPercentage,

                            WeightStatus =
                                list.WeightStatus,

                            CreatedAt =
                                list.CreatedAt,

                            Items =
                                list.PersonalEquipmentDetails
                                    .OrderBy(detail =>
                                        detail.SortOrder)
                                    .Select(detail =>
                                        new
                                        CPersonalEquipmentListDetailItemViewModel
                                        {
                                            EquipmentName =
                                                detail.Equipment != null
                                                    ? detail.Equipment
                                                        .EquipmentName
                                                    : detail.CustomEquipmentName
                                                        ?? "自訂裝備",

                                            CategoryName =
                                                detail.Equipment != null
                                                    ? detail.Equipment
                                                        .Category
                                                        .CategoryName
                                                    : "自訂裝備",

                                            Quantity =
                                                detail.Quantity,

                                            UnitWeightGram =
                                                detail.UnitWeightGram,

                                            TotalWeightGram =
                                                detail.TotalWeightGram,

                                            RequirementLevel =
                                                detail.RequirementLevel
                                                    ?? "",

                                            Notes =
                                                detail.Notes,

                                            IsCustomEquipment =
                                                detail.EquipmentId == null
                                        })
                                    .ToList()
                        })
                    .FirstOrDefault();

            if (vm == null)
            {
                return NotFound();
            }

            return View(vm);
        }

        // STEP 05：軟刪除裝備清單
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            PersonalEquipmentList? list =
                _db.PersonalEquipmentLists
                    .FirstOrDefault(item =>
                        item.ListId == id
                        && !item.IsDeleted);

            if (list == null)
            {
                TempData["ErrorMessage"] =
                    "找不到指定的裝備清單。";

                return RedirectToAction("Index");
            }

            list.IsDeleted = true;
            list.UpdatedAt = DateTime.Now;

            try
            {
                _db.SaveChanges();

                TempData["SuccessMessage"] =
                    $"裝備清單「{list.ListName}」已刪除。";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "刪除失敗：" +
                    ex.GetBaseException().Message;
            }

            return RedirectToAction("Index");
        }

        // STEP 05：載入既有清單進行編輯
        public IActionResult Edit(long id)
        {
            PersonalEquipmentList? list =
                _db.PersonalEquipmentLists
                    .AsNoTracking()
                    .Include(item =>
                        item.Mountain)
                    .Include(item =>
                        item.PersonalEquipmentDetails)
                        .ThenInclude(detail =>
                            detail.Equipment)
                            .ThenInclude(equipment =>
                                equipment!.Category)
                    .FirstOrDefault(item =>
                        item.ListId == id
                        && !item.IsDeleted);

            if (list == null)
            {
                TempData["ErrorMessage"] =
                    "找不到指定的裝備清單。";

                return RedirectToAction("Index");
            }

            CEquipmentConditionViewModel condition =
                new CEquipmentConditionViewModel
                {
                    ListName =
                        list.ListName,

                    MountainId =
                        list.MountainId,

                    HikingDate =
                        list.HikingDate.ToDateTime(
                            TimeOnly.MinValue),

                    HikingDays =
                        list.HikingDays,

                    Season =
                        list.Season,

                    IntensityLevel =
                        list.IntensityLevel ?? "",

                    BodyWeightKg =
                        list.BodyWeightKg,

                    ExperienceLevel =
                        list.ExperienceLevel ?? ""
                };

            List<CPersonalEquipmentItemViewModel> items =
                list.PersonalEquipmentDetails
                    .OrderBy(detail =>
                        detail.SortOrder)
                    .Select(detail =>
                        new
                        CPersonalEquipmentItemViewModel
                        {
                            EquipmentId =
                                detail.EquipmentId,

                            EquipmentName =
                                detail.Equipment != null
                                    ? detail.Equipment
                                        .EquipmentName
                                    : detail.CustomEquipmentName
                                        ?? "自訂裝備",

                            CategoryName =
                                detail.Equipment != null
                                    ? detail.Equipment
                                        .Category
                                        .CategoryName
                                    : "自訂裝備",

                            Quantity =
                                detail.Quantity,

                            UnitWeightGram =
                                detail.UnitWeightGram,

                            RequirementLevel =
                                detail.RequirementLevel
                                    ?? "",

                            Notes =
                                detail.Notes
                        })
                    .ToList();

            if (items.Count == 0)
            {
                TempData["ErrorMessage"] =
                    "這份清單沒有可編輯的裝備明細。";

                return RedirectToAction("Index");
            }

            HttpContext.Session.SetString(
                CDictionary.SK_PERSONAL_EQUIPMENT_EDIT_LIST_ID,
                list.ListId.ToString());

            HttpContext.Session.SetString(
                CDictionary.SK_PERSONAL_EQUIPMENT_CONDITION,
                JsonSerializer.Serialize(condition));

            HttpContext.Session.SetString(
                CDictionary.SK_PERSONAL_EQUIPMENT_ITEMS,
                JsonSerializer.Serialize(items));

            return RedirectToAction("EditItems");
        }

    }
}
