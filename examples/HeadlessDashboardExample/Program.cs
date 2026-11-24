using Blux.UI.Tools;

var controller = new DashboardController("sales");
var kpi = controller.RegisterResource(new DashboardResource<double>("mrr", _ => Task.FromResult(48500.0)));
await controller.RefreshAsync();
Console.WriteLine($"MRR: {DashboardFormat.Format(kpi.Data, DashboardFormatPreset.Compact)}");
