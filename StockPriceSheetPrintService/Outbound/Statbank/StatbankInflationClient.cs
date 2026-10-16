using StockPriceSheetPrintService.Service;
using StockPriceSheetPrintService.Service.Models;
using StockPriceSheetPrintService.Service.Ports.Outbound;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace StockPriceSheetPrintService.Outbound.Statbank
{
	public class StatbankInflationClient(HttpClient client, ILogger<StatbankInflationClient> logger) : IInflationIndexProvider
	{
		// PRIS01, VAREGR 000000 = Forbrugerprisindeks i alt, ENHED 100 = indeks, Tid * = alle udgivne måneder.
		// "*" bruges bevidst: eksplicit at bede om en endnu ikke udgivet måned får hele kaldet til at fejle (EXTRACT-NOTFOUND).
		private static readonly string RequestBody = JsonSerializer.Serialize(new
		{
			table = "PRIS01",
			format = "CSV",
			variables = new object[]
			{
				new { code = "VAREGR", values = new[] { "000000" } },
				new { code = "ENHED", values = new[] { "100" } },
				new { code = "Tid", values = new[] { "*" } },
			},
		});

		public async Task<IReadOnlyList<BenchmarkDataPoint>?> GetConsumerPriceIndexAsync(ClientContext ctx, CancellationToken ct)
		{
			try
			{
				using var content = new StringContent(RequestBody, Encoding.UTF8, "application/json");
				using var response = await client.PostAsync("v1/data", content, ct);
				response.EnsureSuccessStatusCode();

				var csv = await response.Content.ReadAsStringAsync(ct);
				var points = ParseCsv(csv);
				if (points.Count == 0)
				{
					logger.LogError("[INFLATION] Statbank PRIS01 returned no parsable data points - ClientContext {ctx}", ctx);
					return null;
				}
				return points;
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
			{
				logger.LogError(ex, "[INFLATION] Error fetching consumer price index from Statbank - ClientContext {ctx}", ctx);
				return null;
			}
		}

		// CSV: semikolon-separator, evt. BOM, dansk decimalkomma, tid som "2026M09".
		internal static List<BenchmarkDataPoint> ParseCsv(string csv)
		{
			var points = new List<BenchmarkDataPoint>();
			var lines = csv.TrimStart('﻿').Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (lines.Length < 2)
				return points;

			var header = lines[0].Split(';').Select(h => h.Trim('"')).ToList();
			var timeIndex = header.FindIndex(h => h.Equals("TID", StringComparison.OrdinalIgnoreCase));
			var valueIndex = header.FindIndex(h => h.Equals("INDHOLD", StringComparison.OrdinalIgnoreCase));
			if (timeIndex < 0 || valueIndex < 0)
				return points;

			foreach (var line in lines.Skip(1))
			{
				var columns = line.Split(';').Select(c => c.Trim('"')).ToArray();
				if (columns.Length <= Math.Max(timeIndex, valueIndex))
					continue;

				var time = columns[timeIndex];
				// "2026M09" -> 2026-09-01
				if (time.Length != 7 || time[4] != 'M'
					|| !int.TryParse(time.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var year)
					|| !int.TryParse(time.AsSpan(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var month)
					|| month is < 1 or > 12)
					continue;

				if (!double.TryParse(columns[valueIndex].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
					continue;

				points.Add(new BenchmarkDataPoint(new DateTime(year, month, 1), value));
			}

			return points.OrderBy(p => p.Date).ToList();
		}
	}
}
