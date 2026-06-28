using Xunit;

// 各情境共用同一個 InMemory 資料庫（以名稱共用於整個行程），
// 停用平行化以避免播種競爭並確保情境間互不干擾。
[assembly: CollectionBehavior(DisableTestParallelization = true)]
