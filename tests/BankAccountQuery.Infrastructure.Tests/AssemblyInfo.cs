using Xunit;

// EF Core InMemory 以資料庫名稱在整個行程中共用同一份資料，
// 多個 WebApplicationFactory 主機若平行啟動會在播種時競爭（重複鍵）。
// 停用平行化，讓第二個主機看到已播種的共用資料而略過播種。
[assembly: CollectionBehavior(DisableTestParallelization = true)]
