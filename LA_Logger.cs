using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LA_Changeloger
{
    public class LAPickedUpMarker : MonoBehaviour
    {
    }

    public static class LA_Logger
    {
        private static string logFilePath;

        private static int planetScrapTotal = 0;
        private static int planetSpendingTotal = 0;

        private static List<string> currentPlanetShopLogs = new List<string>();

        private static Dictionary<string, int> planetScrapCounts = new Dictionary<string, int>();
        private static Dictionary<string, int> planetScrapValues = new Dictionary<string, int>();

        public static string lastLoggedCartSignature = "";

        static LA_Logger()
        {
            try
            {
                string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string logsFolderPath = Path.Combine(assemblyDir, "logs");

                if (!Directory.Exists(logsFolderPath))
                {
                    Directory.CreateDirectory(logsFolderPath);
                }

                logFilePath = Path.Combine(logsFolderPath, "log.txt");

                File.WriteAllText(logFilePath, string.Empty);
            }
            catch (Exception ex)
            {
            }
        }

        public static void LogDayInfo()
        {
            if (string.IsNullOrEmpty(logFilePath)) return;

            try
            {
                if (TimeOfDay.Instance != null)
                {
                    int currentDay = 0;
                    string planetName = "Undefined";
                    if (StartOfRound.Instance != null)
                    {
                        if (StartOfRound.Instance.gameStats != null)
                        {
                            currentDay = StartOfRound.Instance.gameStats.daysSpent;
                        }

                        if (StartOfRound.Instance.currentLevel != null)
                        {
                            planetName = StartOfRound.Instance.currentLevel.PlanetName;
                            if (string.IsNullOrEmpty(planetName))
                            {
                                planetName = StartOfRound.Instance.currentLevel.PlanetName;
                            }
                        }
                    }

                    int currentQuota = TimeOfDay.Instance.profitQuota;

                    planetScrapTotal = 0;
                    planetScrapCounts.Clear();
                    planetScrapValues.Clear();
                    lastLoggedCartSignature = "";

                    string logLine = $"========================================\n[DAY INFO]\nDay: {currentDay + 1}\nQuota: {currentQuota}\nPlanet: {planetName}";
                    File.AppendAllText(logFilePath, logLine + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static void LogPickedUpScrap(GrabbableObject scrapItem)
        {
            if (scrapItem == null || scrapItem.itemProperties == null) return;

            try
            {
                if (scrapItem.itemProperties.isScrap)
                {
                    if (scrapItem.GetComponent<LAPickedUpMarker>() != null)
                    {
                        return;
                    }

                    scrapItem.gameObject.AddComponent<LAPickedUpMarker>();

                    string itemName = scrapItem.itemProperties.itemName;
                    int scrapValue = scrapItem.scrapValue;

                    planetScrapTotal += scrapValue;

                    if (planetScrapCounts.ContainsKey(itemName))
                    {
                        planetScrapCounts[itemName]++;
                    }
                    else
                    {
                        planetScrapCounts[itemName] = 1;
                    }

                    planetScrapValues[itemName] = scrapValue;
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static void LogShopPurchase(string itemName, int cost, int terminalCredits)
        {
            try
            {
                planetSpendingTotal += cost;

                string logLine = $"{itemName} - cost ({cost}). Credits: {terminalCredits}";
                currentPlanetShopLogs.Add(logLine);
            }
            catch (Exception)
            {
            }
        }

        public static void LogPlanetSummary()
        {
            if (string.IsNullOrEmpty(logFilePath)) return;

            try
            {
                if (currentPlanetShopLogs.Count > 0)
                {
                    File.AppendAllText(logFilePath, "[SHOP INFO]" + Environment.NewLine);
                    foreach (string shopLog in currentPlanetShopLogs)
                    {
                        File.AppendAllText(logFilePath, shopLog + Environment.NewLine);
                    }
                }

                int totalItemsCount = 0;
                foreach (var pair in planetScrapCounts)
                {
                    totalItemsCount += pair.Value;
                }

                if (totalItemsCount > 0)
                {
                    File.AppendAllText(logFilePath, "[PLANET INFO]" + Environment.NewLine);
                    foreach (var pair in planetScrapCounts)
                    {
                        string itemName = pair.Key;
                        int count = pair.Value;
                        int value = planetScrapValues[itemName];

                        string countSuffix = count > 1 ? $" x{count}" : "";
                        File.AppendAllText(logFilePath, $"{itemName}{countSuffix} - cost ({value})" + Environment.NewLine);
                    }
                }

                string summaryLine = $"[SCRAP VALUE] Total: {planetScrapTotal}\n[SPENDING] Total: {planetSpendingTotal}";
                File.AppendAllText(logFilePath, summaryLine + Environment.NewLine);

                if (totalItemsCount > 0)
                {
                    File.AppendAllText(logFilePath, "[SCRAP FIND %]" + Environment.NewLine);
                    foreach (var pair in planetScrapCounts)
                    {
                        string itemName = pair.Key;
                        int count = pair.Value;

                        float findPercentage = ((float)count * 100f) / (float)totalItemsCount;
                        string formattedPercent = findPercentage.ToString("F1", CultureInfo.InvariantCulture);

                        File.AppendAllText(logFilePath, $"{itemName}: {formattedPercent}%" + Environment.NewLine);
                    }
                }

                currentPlanetShopLogs.Clear();
                planetSpendingTotal = 0;

                CalculateGlobalStats();
            }
            catch (Exception ex)
            {
            }
        }

        private static void CalculateGlobalStats()
        {
            try
            {
                if (!File.Exists(logFilePath)) return;

                string entireFile = File.ReadAllText(logFilePath);

                int globalBlockIndex = entireFile.IndexOf("========================================\n[GLOBAL LIFETIME STATS]");
                if (globalBlockIndex != -1)
                {
                    entireFile = entireFile.Substring(0, globalBlockIndex);
                }

                long totalShopSpending = 0;
                long totalScrapValue = 0;

                Dictionary<string, int> globalScrapCounts = new Dictionary<string, int>();
                Dictionary<string, int> globalPlanetCounts = new Dictionary<string, int>();
                Dictionary<string, int> globalShopCounts = new Dictionary<string, int>();

                int totalGlobalScrapCount = 0;
                int totalGlobalPlanetCount = 0;
                int totalGlobalShopCount = 0;

                string[] lines = entireFile.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.None);

                foreach (string line in lines)
                {
                    if (line.StartsWith("Planet: "))
                    {
                        string planet = line.Replace("Planet: ", "").Trim();
                        if (!string.IsNullOrEmpty(planet) && planet != "Undefined")
                        {
                            globalPlanetCounts[planet] = globalPlanetCounts.ContainsKey(planet) ? globalPlanetCounts[planet] + 1 : 1;
                            totalGlobalPlanetCount++;
                        }
                    }

                    if (line.StartsWith("[SHOP] "))
                    {
                        var match = Regex.Match(line, @"\[SHOP\]\s+(.+?)\s+-\s+cost\s+\((\d+)\)");
                        if (match.Success)
                        {
                            string itemName = match.Groups[1].Value.Trim();
                            int cost = int.Parse(match.Groups[2].Value);

                            totalShopSpending += cost;
                            globalShopCounts[itemName] = globalShopCounts.ContainsKey(itemName) ? globalShopCounts[itemName] + 1 : 1;
                            totalGlobalShopCount++;
                        }
                    }

                    if (line.StartsWith("[SCRAP VALUE] Total: "))
                    {
                        var match = Regex.Match(line, @"\[SCRAP VALUE\] Total:\s+(\d+)");
                        if (match.Success)
                        {
                            totalScrapValue += int.Parse(match.Groups[1].Value);
                        }
                    }

                    if (line.Contains(" - cost (") && !line.StartsWith("[SHOP]") && !line.StartsWith("[SCRAP") && !line.StartsWith("Day:"))
                    {
                        var match = Regex.Match(line, @"^\s*(.+?)(?:\s+x(\d+))?\s+-\s+cost");
                        if (match.Success)
                        {
                            string itemName = match.Groups[1].Value.Trim();
                            int count = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1;

                            globalScrapCounts[itemName] = globalScrapCounts.ContainsKey(itemName) ? globalScrapCounts[itemName] + count : count;
                            totalGlobalScrapCount += count;
                        }
                    }
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("========================================");
                sb.AppendLine("[GLOBAL LIFETIME STATS]");
                sb.AppendLine($"[TOTAL SHOP SPENDING]: {totalShopSpending}");
                sb.AppendLine($"[TOTAL SCRAP VALUE]: {totalScrapValue}");

                sb.AppendLine("[TOTAL SCRAP FIND %]");
                if (totalGlobalScrapCount > 0)
                {
                    foreach (var pair in globalScrapCounts)
                    {
                        float percent = (pair.Value * 100f) / totalGlobalScrapCount;
                        sb.AppendLine($"  {pair.Key}: {percent.ToString("F1", CultureInfo.InvariantCulture)}%");
                    }
                }
                else sb.AppendLine("  No scrap collected yet.");

                sb.AppendLine("[TOTAL PLANET ROUTING %]");
                if (totalGlobalPlanetCount > 0)
                {
                    foreach (var pair in globalPlanetCounts)
                    {
                        float percent = (pair.Value * 100f) / totalGlobalPlanetCount;
                        sb.AppendLine($"  {pair.Key}: {percent.ToString("F1", CultureInfo.InvariantCulture)}%");
                    }
                }
                else sb.AppendLine("  No planets visited yet.");

                sb.AppendLine("[TOTAL SHOP USES %]");
                if (totalGlobalShopCount > 0)
                {
                    foreach (var pair in globalShopCounts)
                    {
                        float percent = (pair.Value * 100f) / totalGlobalShopCount;
                        sb.AppendLine($"  {pair.Key}: {percent.ToString("F1", CultureInfo.InvariantCulture)}%");
                    }
                }
                else sb.AppendLine("  No shop items purchased yet.");

                File.WriteAllText(logFilePath, entireFile + sb.ToString());
            }
            catch (Exception)
            {
            }
        }

        public static void Initialize() { }
    }

    [HarmonyPatch(typeof(StartOfRound), "StartGame")]
    public class LALoggerDayInfoPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            LA_Logger.LogDayInfo();
        }
    }

    [HarmonyPatch(typeof(GrabbableObject), "GrabItemOnClient")]
    public class LALoggerGrabObjectPatch
    {
        [HarmonyPostfix]
        public static void Postfix(GrabbableObject __instance)
        {
            LA_Logger.LogPickedUpScrap(__instance);
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "ShipLeave")]
    public class LALoggerRoundEndPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            LA_Logger.LogPlanetSummary();
        }
    }

    [HarmonyPatch(typeof(Terminal), "LoadNewNode")]
    public class LALoggerTerminalShopPatch
    {
        private static int previousCartCount = 0;

        [HarmonyPrefix]
        public static void Prefix(Terminal __instance, TerminalNode node)
        {
            try
            {
                if (__instance.orderedItemsFromTerminal == null) return;

                if (node != null && node.isConfirmationNode)
                {
                    previousCartCount = __instance.orderedItemsFromTerminal.Count;
                    return;
                }

                if (__instance.currentNode != null && __instance.currentNode.isConfirmationNode)
                {
                    int currentCount = __instance.orderedItemsFromTerminal.Count;

                    if (currentCount > previousCartCount)
                    {
                        for (int i = previousCartCount; i < currentCount; i++)
                        {
                            int itemIndex = __instance.orderedItemsFromTerminal[i];

                            if (__instance.buyableItemsList != null && itemIndex >= 0 && itemIndex < __instance.buyableItemsList.Length)
                            {
                                Item shopItem = __instance.buyableItemsList[itemIndex];
                                if (shopItem != null)
                                {
                                    string name = shopItem.itemName;
                                    int price = shopItem.creditsWorth;

                                    int finalCost = price;
                                    if (__instance.itemSalesPercentages != null && itemIndex < __instance.itemSalesPercentages.Length)
                                    {
                                        finalCost = Mathf.RoundToInt((float)price * ((float)__instance.itemSalesPercentages[itemIndex] / 100f));
                                    }

                                    int actualRemainingCredits = __instance.groupCredits;

                                    LA_Logger.LogShopPurchase(name, finalCost, actualRemainingCredits);
                                }
                            }
                        }
                    }

                    previousCartCount = currentCount;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}