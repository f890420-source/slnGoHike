using System.Net;
using System.Text.Json.Nodes;
using prjGoHike.Models;

static class TrailFeatureApiChecks
{
    public static async Task RunAsync(TestDataContext context,
        Func<HttpMethod, string, JsonNode?, HttpStatusCode, Task<JsonNode?>> send,
        Action<string?, string> authenticate, Action<bool, string> check)
    {
        const string path = "/api/trailfeatures";
        var trail = new Trail { TrailId = 81001, TrailName = "Published trail", IsPublished = true };
        var hiddenTrail = new Trail { TrailId = 81002, TrailName = "Hidden trail", IsPublished = false };
        context.Trails.Add(trail);
        context.Trails.Add(hiddenTrail);
        var report = JsonNode.Parse("""
            {"trailId":81001,"featureType":"WaterSource","featureName":" 山泉 ",
             "location":{"type":"Point","coordinates":[121.5,24.5,1200]},"featureDescription":" 路旁水源 "}
            """)!.AsObject();

        foreach (var role in new string?[] { null, "Member" })
        {
            authenticate(role, "1");
            var denied = role is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
            await send(HttpMethod.Get, path + "/admin", null, denied);
            await send(HttpMethod.Get, path + "/admin/1", null, denied);
            await send(HttpMethod.Put, path + "/1", report, denied);
            await send(HttpMethod.Delete, path + "/1", null, denied);
            if (role is null) await send(HttpMethod.Post, path, report, HttpStatusCode.Unauthorized);
        }
        var saves = context.Saves;
        foreach (var invalidId in new[] { "0", "-1", "invalid" })
        {
            authenticate("Member", invalidId);
            await send(HttpMethod.Post, path, report, HttpStatusCode.Unauthorized);
        }
        authenticate("Member", "1");
        await send(HttpMethod.Post, path, new JsonObject(), HttpStatusCode.BadRequest);
        foreach (var (field, value) in new (string, JsonNode?)[]
        {
            ("trailId", JsonValue.Create(0)), ("trailId", JsonValue.Create(-1)),
            ("featureType", JsonValue.Create("水源")), ("featureType", JsonValue.Create("bad type")),
            ("featureType", JsonValue.Create(new string('x', 21))), ("featureType", null),
            ("featureName", JsonValue.Create(" ")), ("featureName", null),
            ("featureName", JsonValue.Create(new string('x', 121))),
            ("featureDescription", JsonValue.Create(new string('x', 1001))), ("location", null),
            ("location", JsonNode.Parse("""{"type":"Point","coordinates":[]}""")),
            ("location", JsonNode.Parse("""{"type":"Point","coordinates":[181,24]}""")),
            ("location", JsonNode.Parse("""{"type":"Point","coordinates":[121,91]}""")),
            ("location", JsonNode.Parse("""{"type":"Point","coordinates":[121,24,1,2]}""")),
            ("location", JsonNode.Parse("""{"type":"LineString","coordinates":[[121,24],[121.1,24.1]]}""")),
            ("location", JsonNode.Parse("""{"type":"Feature","geometry":{"type":"Point","coordinates":[121,24]}}"""))
        })
        {
            var bad = report.DeepClone().AsObject(); bad[field] = value;
            await send(HttpMethod.Post, path, bad, HttpStatusCode.BadRequest);
        }
        foreach (var trailId in new[] { 81002, 999999 })
        {
            var bad = report.DeepClone().AsObject(); bad["trailId"] = trailId;
            await send(HttpMethod.Post, path, bad, HttpStatusCode.NotFound);
        }
        check(context.Saves == saves, "Rejected feature reports must not be persisted.");

        var overpost = report.DeepClone().AsObject();
        overpost["featureId"] = 999999; overpost["isAvailable"] = true;
        overpost["reliabilityLevel"] = 5; overpost["dataSource"] = "Official";
        var created = (await send(HttpMethod.Post, path, overpost, HttpStatusCode.Created))!["data"]!.AsObject();
        var id = created["featureId"]!.GetValue<long>();
        check(id > 0 && id != 999999 && context.Saves == saves + 1, "Report saves once with a server generated ID.");
        check(!created["isAvailable"]!.GetValue<bool>() && created["reliabilityLevel"]!.GetValue<int>() == 1
            && created["dataSource"]!.GetValue<string>() == "Member report", "Report cannot overpost trusted fields.");
        check(created["featureName"]!.GetValue<string>() == "山泉"
            && created["featureDescription"]!.GetValue<string>() == "路旁水源", "Report text is trimmed.");
        check(created["location"]!["type"]!.GetValue<string>() == "Point"
            && created["location"]!["coordinates"]![2]!.GetValue<int>() == 1200, "Location round trips as GeoJSON with elevation.");
        check(context.TrailFeatures.Single(x => x.FeatureId == id).Location.SRID == 4326, "Location uses SRID 4326.");
        check(!created.ContainsKey("trail"), "Response excludes entity navigation properties.");
        authenticate(null, "1");
        await send(HttpMethod.Get, path + "/" + id, null, HttpStatusCode.NotFound);
        check((await send(HttpMethod.Get, path, null, HttpStatusCode.OK))!["data"]!.AsArray().Count == 0,
            "New reports are hidden from anonymous callers.");

        authenticate("Admin", "1");
        var detail = (await send(HttpMethod.Get, path + "/admin/" + id, null, HttpStatusCode.OK))!["data"]!;
        check(JsonNode.DeepEquals(created, detail), "Admin detail includes every stored feature field.");
        check((await send(HttpMethod.Get, path + "/admin?trailId=81001&featureType=WaterSource", null, HttpStatusCode.OK))!["data"]!.AsArray().Count == 1,
            "Admin list includes unavailable reports and supports combined filters.");
        check((await send(HttpMethod.Get, path + "/admin?isAvailable=false", null, HttpStatusCode.OK))!["data"]!.AsArray().Count == 1,
            "Admin can filter unavailable reports.");
        check((await send(HttpMethod.Get, path + "/admin?isAvailable=true", null, HttpStatusCode.OK))!["data"]!.AsArray().Count == 0,
            "Admin availability filter excludes unavailable reports.");
        await send(HttpMethod.Get, path + "/admin?isAvailable=invalid", null, HttpStatusCode.BadRequest);
        var update = report.DeepClone().AsObject();
        update["reliabilityLevel"] = 4; update["isAvailable"] = true; update["dataSource"] = " 現地確認 ";
        saves = context.Saves;
        foreach (var (field, value) in new (string, JsonNode?)[]
        {
            ("reliabilityLevel", JsonValue.Create(0)), ("reliabilityLevel", JsonValue.Create(6)),
            ("isAvailable", null), ("featureName", JsonValue.Create(" ")),
            ("dataSource", JsonValue.Create(new string('x', 201))),
            ("location", JsonNode.Parse("""{"type":"MultiPoint","coordinates":[[121,24]]}""")),
            ("location", JsonNode.Parse("""{"type":"Point","coordinates":[121,-91]}"""))
        })
        {
            var bad = update.DeepClone().AsObject(); bad[field] = value;
            await send(HttpMethod.Put, path + "/" + id, bad, HttpStatusCode.BadRequest);
        }
        var omitted = update.DeepClone().AsObject(); omitted.Remove("isAvailable");
        await send(HttpMethod.Put, path + "/" + id, omitted, HttpStatusCode.BadRequest);
        omitted = update.DeepClone().AsObject(); omitted.Remove("reliabilityLevel");
        await send(HttpMethod.Put, path + "/" + id, omitted, HttpStatusCode.BadRequest);
        var missingTrail = update.DeepClone().AsObject(); missingTrail["trailId"] = 999999;
        await send(HttpMethod.Put, path + "/" + id, missingTrail, HttpStatusCode.NotFound);
        await send(HttpMethod.Put, path + "/999999", update, HttpStatusCode.NotFound);
        check(context.Saves == saves && !context.TrailFeatures.Single(x => x.FeatureId == id).IsAvailable,
            "Invalid updates do not save or mutate the report.");
        var updated = (await send(HttpMethod.Put, path + "/" + id, update, HttpStatusCode.OK))!["data"]!;
        check(context.Saves == saves + 1 && updated["reliabilityLevel"]!.GetValue<int>() == 4
            && updated["dataSource"]!.GetValue<string>() == "現地確認", "Admin can confirm reliability, source and availability.");
        authenticate(null, "1");
        var publicDetail = (await send(HttpMethod.Get, path + "/" + id, null, HttpStatusCode.OK))!["data"]!;
        check(JsonNode.DeepEquals(updated, publicDetail), "Available reports on published trails are public.");
        check((await send(HttpMethod.Get, path + "?trailId=81001&featureType=WaterSource", null, HttpStatusCode.OK))!["data"]!.AsArray().Count == 1,
            "Public combined filters match the report.");
        foreach (var query in new[] { "trailId=81002", "trailId=999999", "featureType=Hazard", "trailId=81002&featureType=WaterSource" })
            check((await send(HttpMethod.Get, path + "?" + query, null, HttpStatusCode.OK))!["data"]!.AsArray().Count == 0,
                "Unmatched filters return a successful empty array.");
        foreach (var role in new string?[] { null, "Admin" })
        {
            authenticate(role, "1");
            var listPath = role is null ? path : path + "/admin";
            foreach (var query in new[] { "trailId=0", "trailId=-1", "trailId=abc", "featureType=bad%20type", "featureType=" + new string('x', 21) })
                await send(HttpMethod.Get, listPath + "?" + query, null, HttpStatusCode.BadRequest);
        }
        authenticate("Admin", "1");
        update["trailId"] = hiddenTrail.TrailId;
        update.Remove("featureDescription"); update.Remove("dataSource");
        var moved = (await send(HttpMethod.Put, path + "/" + id, update, HttpStatusCode.OK))!["data"]!;
        check(moved["featureDescription"] is null && moved["dataSource"] is null, "PUT clears omitted optional text fields.");
        authenticate(null, "1");
        await send(HttpMethod.Get, path + "/" + id, null, HttpStatusCode.NotFound);
        check((await send(HttpMethod.Get, path, null, HttpStatusCode.OK))!["data"]!.AsArray().Count == 0,
            "Available features on unpublished trails remain hidden.");
        authenticate("Admin", "1");
        check((await send(HttpMethod.Get, path + "/admin?trailId=81002", null, HttpStatusCode.OK))!["data"]!.AsArray().Count == 1,
            "Admin can manage features associated with unpublished trails.");
        update["trailId"] = trail.TrailId; update["isAvailable"] = false;
        await send(HttpMethod.Put, path + "/" + id, update, HttpStatusCode.OK);
        authenticate(null, "1");
        await send(HttpMethod.Get, path + "/" + id, null, HttpStatusCode.NotFound);
        authenticate("Admin", "1");
        context.FailSave = true;
        try
        {
            foreach (var (method, url, body) in new[]
            {
                (HttpMethod.Post, path, (JsonNode?)report),
                (HttpMethod.Put, path + "/" + id, (JsonNode?)update),
                (HttpMethod.Delete, path + "/" + id, (JsonNode?)null)
            })
            {
                var failure = await send(method, url, body, HttpStatusCode.InternalServerError);
                check(!failure!.ToJsonString().Contains("private database details"), "Feature persistence failures do not expose database details.");
            }
        }
        finally { context.FailSave = false; }
        // The memory double does not roll back a failed Remove; restore the item for the successful delete check.
        context.TrailFeatures.Add(new TrailFeature { FeatureId = id, TrailId = trail.TrailId, Trail = trail });
        saves = context.Saves;
        var deleted = await send(HttpMethod.Delete, path + "/" + id, null, HttpStatusCode.OK);
        check(context.Saves == saves + 1 && deleted!["success"]!.GetValue<bool>(), "Delete persists and returns the existing success envelope.");
        await send(HttpMethod.Delete, path + "/" + id, null, HttpStatusCode.NotFound);
        await send(HttpMethod.Get, path + "/admin/" + id, null, HttpStatusCode.NotFound);
        await send(HttpMethod.Get, path + "/999999", null, HttpStatusCode.NotFound);
        foreach (var url in new[] { path + "/0", path + "/admin/0" })
            await send(HttpMethod.Get, url, null, HttpStatusCode.BadRequest);
        await send(HttpMethod.Put, path + "/0", update, HttpStatusCode.BadRequest);
        await send(HttpMethod.Delete, path + "/0", null, HttpStatusCode.BadRequest);
    }
}
