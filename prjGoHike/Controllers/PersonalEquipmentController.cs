using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using prjGoHike.Models;
using prjGoHike.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace prjGoHike.Controllers
{
    public class PersonalEquipmentController : Controller
    {
        private readonly GoHikeDataContext _db;

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

        public IActionResult TestDatabase()
        {
            int mountainCount = _db.Mountains.Count();

            return Content(
                $"資料庫連線成功，目前共有 {mountainCount} 筆山岳資料。");
        }
        // GET：顯示表單
        public IActionResult Create()
        {
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveList()
        {
            long? memberId =
                GetCurrentUserId();

            if (!memberId.HasValue)
            {
                TempData["ErrorMessage"] =
                    "請先登入後再儲存裝備清單。";

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

            return Content(
                $"儲存前檢查成功。" +
                $"會員編號：{memberId.Value}，" +
                $"清單名稱：{condition.ListName}，" +
                $"裝備項目：{items.Count} 筆。");
        }

    }
}
