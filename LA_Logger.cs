using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;

namespace LA_Changeloger
{
    public class LAPickedUpMarker : MonoBehaviour
    {
        public string firstFinderName = "Unknown";
    }

    public class ActiveFight
    {
        public float startTime;
        public float lastHitTime;

        public ActiveFight(float currentTime)
        {
            startTime = currentTime;
            lastHitTime = currentTime;
        }

        public int GetDuration(float currentTime)
        {
            return Mathf.RoundToInt(currentTime - startTime);
        }
    }

    public static class LA_Logger
    {
        private static string logFilePath;
        private static string statsFilePath;
        private static bool hasLoggedFirstEntry = false;

        private static List<string> currentPlanetScrap = new List<string>();
        private static List<int> currentPlanetValues = new List<int>();
        private static HashSet<ulong> loggedNetworkObjectIds = new HashSet<ulong>();

        private static Dictionary<string, ActiveFight> activeFights = new Dictionary<string, ActiveFight>();

        private static int totalRoutes = 0;
        private static Dictionary<string, int> globalRoutes = new Dictionary<string, int>();

        private static int totalWeathers = 0;
        private static Dictionary<string, int> globalWeathers = new Dictionary<string, int>();

        private static int totalScrapPicks = 0;
        private static Dictionary<string, int> globalScrapPicks = new Dictionary<string, int>();

        private static int totalShopPurchases = 0;
        private static Dictionary<string, int> globalShopPurchases = new Dictionary<string, int>();

        private static int totalSpawnedCreatures = 0;
        private static Dictionary<string, int> globalCreaturesSpawned = new Dictionary<string, int>();

        private static int totalDamageEvents = 0;
        private static Dictionary<string, int> globalDamageLogs = new Dictionary<string, int>();

        private static int totalDeathEvents = 0;
        private static Dictionary<string, int> globalDeaths = new Dictionary<string, int>();

        private static int totalHitEvents = 0;
        private static Dictionary<string, int> globalFights = new Dictionary<string, int>();

        private static List<string> currentPlanetShopLogs = new List<string>();
        private static List<string> currentPlanetFightLogs = new List<string>();
        private static int initialCreditsBeforePurchase = 0;
        private static bool isFirstPurchaseThisPlanet = true;

        private static List<string> currentPlanetDamageLogs = new List<string>();

        private static List<string> currentPlanetDeathLogs = new List<string>();

        private static EnemyAI[] cachedEnemies;
        private static float cachedEnemiesFrameTime = float.MinValue;

        public static EnemyAI[] GetCachedEnemies()
        {
            float now = Time.time;
            if (cachedEnemies == null || now != cachedEnemiesFrameTime)
            {
                cachedEnemies = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
                cachedEnemiesFrameTime = now;
            }
            return cachedEnemies;
        }

        public static EnemyAI FindClosestEnemy(Vector3 position, float maxDistance)
        {
            EnemyAI[] allEnemies = GetCachedEnemies();
            if (allEnemies == null) return null;

            float minDistance = maxDistance;
            EnemyAI closest = null;

            foreach (EnemyAI enemy in allEnemies)
            {
                if (enemy == null || enemy.isEnemyDead) continue;

                float dist = Vector3.Distance(enemy.transform.position, position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = enemy;
                }
            }

            return closest;
        }

        public static string GetEnemyDisplayName(EnemyAI enemy)
        {
            if (enemy == null) return "Unknown Enemy";

            if (enemy.enemyType != null && !string.IsNullOrEmpty(enemy.enemyType.enemyName))
            {
                return enemy.enemyType.enemyName;
            }

            return enemy.gameObject.name.Replace("(Clone)", "").Trim();
        }

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
                statsFilePath = Path.Combine(logsFolderPath, "global_stats.txt");

                if (!File.Exists(logFilePath))
                {
                    File.WriteAllText(logFilePath, string.Empty);
                    hasLoggedFirstEntry = false;
                }
                else
                {
                    hasLoggedFirstEntry = new FileInfo(logFilePath).Length > 0;
                }

                LoadGlobalStats();
            }
            catch (Exception) { }
        }

        public static void Initialize() { }

        public static void ResetScrapList()
        {
            currentPlanetScrap.Clear();
            currentPlanetValues.Clear();
            loggedNetworkObjectIds.Clear();

            currentPlanetShopLogs.Clear();
            initialCreditsBeforePurchase = 0;
            isFirstPurchaseThisPlanet = true;

            currentPlanetDamageLogs.Clear();

            currentPlanetDeathLogs.Clear();
            currentPlanetFightLogs.Clear();

            activeFights.Clear();
        }

        private static void LoadIntoDictionary(string key, string prefix, Dictionary<string, int> dict, int value)
        {
            if (!key.StartsWith(prefix)) return;
            string name = key.Substring(prefix.Length);
            dict[name] = value;
        }

        private static void LoadGlobalStats()
        {
            try
            {
                if (string.IsNullOrEmpty(statsFilePath) || !File.Exists(statsFilePath)) return;

                foreach (string rawLine in File.ReadAllLines(statsFilePath))
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    int splitIndex = line.IndexOf('=');
                    if (splitIndex < 0) continue;

                    string key = line.Substring(0, splitIndex);
                    string valueText = line.Substring(splitIndex + 1);

                    if (!int.TryParse(valueText, out int value)) continue;

                    switch (key)
                    {
                        case "TOTAL_ROUTES": totalRoutes = value; continue;
                        case "TOTAL_WEATHERS": totalWeathers = value; continue;
                        case "TOTAL_SCRAP_PICKS": totalScrapPicks = value; continue;
                        case "TOTAL_SHOP_PURCHASES": totalShopPurchases = value; continue;
                        case "TOTAL_SPAWNED_CREATURES": totalSpawnedCreatures = value; continue;
                        case "TOTAL_DAMAGE_EVENTS": totalDamageEvents = value; continue;
                        case "TOTAL_DEATH_EVENTS": totalDeathEvents = value; continue;
                        case "TOTAL_HIT_EVENTS": totalHitEvents = value; continue;
                    }

                    LoadIntoDictionary(key, "ROUTE:", globalRoutes, value);
                    LoadIntoDictionary(key, "WEATHER:", globalWeathers, value);
                    LoadIntoDictionary(key, "SCRAP:", globalScrapPicks, value);
                    LoadIntoDictionary(key, "SHOP:", globalShopPurchases, value);
                    LoadIntoDictionary(key, "CREATURE:", globalCreaturesSpawned, value);
                    LoadIntoDictionary(key, "DAMAGE:", globalDamageLogs, value);
                    LoadIntoDictionary(key, "DEATH:", globalDeaths, value);
                    LoadIntoDictionary(key, "FIGHT:", globalFights, value);
                }
            }
            catch (Exception) { }
        }

        private static void SaveGlobalStats()
        {
            try
            {
                if (string.IsNullOrEmpty(statsFilePath)) return;

                StringBuilder sb = new StringBuilder(1024);

                sb.Append($"TOTAL_ROUTES={totalRoutes}{Environment.NewLine}");
                foreach (var kvp in globalRoutes) sb.Append($"ROUTE:{kvp.Key}={kvp.Value}{Environment.NewLine}");

                sb.Append($"TOTAL_WEATHERS={totalWeathers}{Environment.NewLine}");
                foreach (var kvp in globalWeathers) sb.Append($"WEATHER:{kvp.Key}={kvp.Value}{Environment.NewLine}");

                sb.Append($"TOTAL_SCRAP_PICKS={totalScrapPicks}{Environment.NewLine}");
                foreach (var kvp in globalScrapPicks) sb.Append($"SCRAP:{kvp.Key}={kvp.Value}{Environment.NewLine}");

                sb.Append($"TOTAL_SHOP_PURCHASES={totalShopPurchases}{Environment.NewLine}");
                foreach (var kvp in globalShopPurchases) sb.Append($"SHOP:{kvp.Key}={kvp.Value}{Environment.NewLine}");

                sb.Append($"TOTAL_SPAWNED_CREATURES={totalSpawnedCreatures}{Environment.NewLine}");
                foreach (var kvp in globalCreaturesSpawned) sb.Append($"CREATURE:{kvp.Key}={kvp.Value}{Environment.NewLine}");

                sb.Append($"TOTAL_DAMAGE_EVENTS={totalDamageEvents}{Environment.NewLine}");
                foreach (var kvp in globalDamageLogs) sb.Append($"DAMAGE:{kvp.Key}={kvp.Value}{Environment.NewLine}");

                sb.Append($"TOTAL_DEATH_EVENTS={totalDeathEvents}{Environment.NewLine}");
                foreach (var kvp in globalDeaths) sb.Append($"DEATH:{kvp.Key}={kvp.Value}{Environment.NewLine}");

                sb.Append($"TOTAL_HIT_EVENTS={totalHitEvents}{Environment.NewLine}");
                foreach (var kvp in globalFights) sb.Append($"FIGHT:{kvp.Key}={kvp.Value}{Environment.NewLine}");

                File.WriteAllText(statsFilePath, sb.ToString());
            }
            catch (Exception) { }
        }

        private static void AppendRankedSection(StringBuilder sb, string title, Dictionary<string, int> data, int total, string rateLabel)
        {
            sb.Append($"{Environment.NewLine}{Environment.NewLine}{title}:");

            if (data.Count > 0)
            {
                var sorted = new List<KeyValuePair<string, int>>(data);
                sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

                int maxValue = sorted[0].Value;

                foreach (var kvp in sorted)
                {
                    int rate = total > 0 ? Mathf.RoundToInt((float)kvp.Value / total * 100f) : 0;
                    string indent = kvp.Value == maxValue ? "        " : "";
                    sb.Append($"{Environment.NewLine}{indent}- {kvp.Key}, {rateLabel}: {rate}%");
                }
            }
            else
            {
                sb.Append($"{Environment.NewLine}- No data");
            }
        }

        public static void LogPickedUpScrap(GrabbableObject scrapItem)
        {
            if (scrapItem == null || scrapItem.itemProperties == null) return;

            try
            {
                if (scrapItem.itemProperties.isScrap)
                {
                    if (scrapItem.scrapValue <= 0) return;

                    var netObj = scrapItem.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        if (loggedNetworkObjectIds.Contains(netObj.NetworkObjectId)) return;
                        loggedNetworkObjectIds.Add(netObj.NetworkObjectId);
                    }

                    if (scrapItem.GetComponent<LAPickedUpMarker>() != null) return;

                    string finderName = "Unknown";
                    if (scrapItem.playerHeldBy != null)
                    {
                        finderName = scrapItem.playerHeldBy.playerUsername;
                    }
                    else if (StartOfRound.Instance != null && StartOfRound.Instance.localPlayerController != null)
                    {
                        var localPlayer = StartOfRound.Instance.localPlayerController;
                        if (localPlayer.isHoldingObject && localPlayer.currentlyHeldObjectServer == scrapItem)
                        {
                            finderName = localPlayer.playerUsername;
                        }
                    }

                    var marker = scrapItem.gameObject.AddComponent<LAPickedUpMarker>();
                    marker.firstFinderName = finderName;

                    string itemName = scrapItem.itemProperties.itemName;
                    int scrapValue = scrapItem.scrapValue;

                    currentPlanetScrap.Add($"- {itemName} (${scrapValue}) - found {finderName}");
                    currentPlanetValues.Add(scrapValue);

                    totalScrapPicks++;
                    if (!globalScrapPicks.ContainsKey(itemName)) globalScrapPicks[itemName] = 0;
                    globalScrapPicks[itemName]++;
                }
            }
            catch (Exception) { }
        }

        public static void LogShopPurchase(string itemName, int cost, int creditsBefore, int creditsAfter)
        {
            try
            {
                if (isFirstPurchaseThisPlanet)
                {
                    initialCreditsBeforePurchase = creditsBefore;
                    isFirstPurchaseThisPlanet = false;
                }

                currentPlanetShopLogs.Add($"- {itemName} (${cost}) - money left (${creditsAfter})");

                totalShopPurchases++;
                if (!globalShopPurchases.ContainsKey(itemName)) globalShopPurchases[itemName] = 0;
                globalShopPurchases[itemName]++;
            }
            catch (Exception) { }
        }

        public static void LogPlayerDamage(string username, int damageAmount, string sourceName, string enemyName)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || damageAmount <= 0) return;

                if (sourceName == "Fall Damage" || sourceName == "Gravity")
                {
                    currentPlanetDamageLogs.Add($"- Player {username} took {damageAmount} damage, source: {sourceName}");
                }
                else
                {
                    currentPlanetDamageLogs.Add($"- Player {username} took {damageAmount} damage, source: {sourceName}, enemy: {enemyName}");
                }

                string damageKey = enemyName != "None"
                    ? $"{username}, enemy: {enemyName}"
                    : $"{username}, source: {sourceName}";

                totalDamageEvents++;
                if (!globalDamageLogs.ContainsKey(damageKey)) globalDamageLogs[damageKey] = 0;
                globalDamageLogs[damageKey]++;
            }
            catch (Exception) { }
        }

        public static void LogPlayerAttack(string attackerName, int damageAmount, string targetName, int durationSeconds, string remainingHP)
        {
            try
            {
                if (string.IsNullOrEmpty(attackerName) || damageAmount <= 0) return;

                currentPlanetFightLogs.Add($"- {attackerName} dealt {damageAmount} damage to {targetName}, {durationSeconds} seconds, remaining HP: {remainingHP}");

                totalHitEvents++;
                if (!globalFights.ContainsKey(targetName)) globalFights[targetName] = 0;
                globalFights[targetName]++;
            }
            catch (Exception) { }
        }

        public static int UpdateAndGetFightDuration(string participantA, string participantB)
        {
            try
            {
                string fightKey = string.Compare(participantA, participantB) < 0
    ? $"{participantA}_{participantB}"
    : $"{participantB}_{participantA}";

                float currentTime = Time.time;
                if (activeFights.ContainsKey(fightKey))
                {
                    ActiveFight fight = activeFights[fightKey];

                    if (currentTime - fight.lastHitTime > 15f)
                    {
                        activeFights[fightKey] = new ActiveFight(currentTime);
                        return 0;
                    }

                    fight.lastHitTime = currentTime;
                    return fight.GetDuration(currentTime);
                }
                else
                {
                    activeFights[fightKey] = new ActiveFight(currentTime);
                    return 0;
                }
            }
            catch (Exception) { return 0; }
        }

        public static void LogPlayerDeath(string username, string timeText, string locationText, string causeText)
        {
            try
            {
                if (string.IsNullOrEmpty(username)) return;

                currentPlanetDeathLogs.Add($"- {username} died ({causeText}), time: {timeText}, where: {locationText}");

                totalDeathEvents++;
                if (!globalDeaths.ContainsKey(username)) globalDeaths[username] = 0;
                globalDeaths[username]++;
            }
            catch (Exception) { }
        }

        public static void LogPlanetSummary()
        {
            if (string.IsNullOrEmpty(logFilePath)) return;

            try
            {
                EnemyAI[] allEnemies = GetCachedEnemies();
                List<string> enemyLogs = new List<string>();

                if (allEnemies != null)
                {
                    foreach (EnemyAI enemy in allEnemies)
                    {
                        if (enemy == null || enemy.enemyType == null) continue;

                        string enemyName = enemy.enemyType.enemyName;
                        if (string.IsNullOrEmpty(enemyName)) enemyName = enemy.gameObject.name.Replace("(Clone)", "").Trim();

                        string healthStatus;
                        if (enemy.isEnemyDead)
                        {
                            healthStatus = "0 (Dead)";
                        }
                        else if (enemy.enemyHP <= 0)
                        {
                            healthStatus = "Immortal";
                        }
                        else
                        {
                            healthStatus = enemy.enemyHP.ToString();
                        }

                        enemyLogs.Add($"- {enemyName}, HP: {healthStatus}");

                        totalSpawnedCreatures++;
                        if (!globalCreaturesSpawned.ContainsKey(enemyName)) globalCreaturesSpawned[enemyName] = 0;
                        globalCreaturesSpawned[enemyName]++;
                    }
                }

                if (currentPlanetScrap.Count == 0 && currentPlanetShopLogs.Count == 0 && enemyLogs.Count == 0 && currentPlanetDamageLogs.Count == 0 && currentPlanetDeathLogs.Count == 0 && currentPlanetFightLogs.Count == 0) return;

                if (StartOfRound.Instance != null && StartOfRound.Instance.currentLevel != null)
                {
                    string planetName = StartOfRound.Instance.currentLevel.PlanetName;
                    string weather = StartOfRound.Instance.currentLevel.currentWeather.ToString();

                    if (string.IsNullOrEmpty(planetName)) planetName = "Undefined";

                    totalRoutes++;
                    if (!globalRoutes.ContainsKey(planetName)) globalRoutes[planetName] = 0;
                    globalRoutes[planetName]++;

                    totalWeathers++;
                    if (!globalWeathers.ContainsKey(weather)) globalWeathers[weather] = 0;
                    globalWeathers[weather]++;

                    int totalScrapValue = 0;
                    foreach (int value in currentPlanetValues)
                    {
                        totalScrapValue += value;
                    }

                    StringBuilder sb = new StringBuilder(2048);

                    if (hasLoggedFirstEntry)
                    {
                        sb.Append(Environment.NewLine);
                    }

                    sb.Append($"PLANET_NAME: {planetName}{Environment.NewLine}WEATHER: {weather}{Environment.NewLine}");
                    sb.Append($"--------------------------------{Environment.NewLine}SCRAP (${totalScrapValue}):");

                    if (currentPlanetScrap.Count > 0)
                    {
                        foreach (string scrapInfo in currentPlanetScrap)
                        {
                            sb.Append(Environment.NewLine).Append(scrapInfo);
                        }
                    }
                    else
                    {
                        sb.Append(Environment.NewLine).Append("- No scrap collected");
                    }

                    if (currentPlanetShopLogs.Count > 0)
                    {
                        sb.Append($"{Environment.NewLine}SHOP - CREDITS ({initialCreditsBeforePurchase}):");
                        foreach (string shopInfo in currentPlanetShopLogs)
                        {
                            sb.Append(Environment.NewLine).Append(shopInfo);
                        }
                    }

                    sb.Append($"{Environment.NewLine}CREATURES ({enemyLogs.Count}):");
                    if (enemyLogs.Count > 0)
                    {
                        foreach (string enemyInfo in enemyLogs)
                        {
                            sb.Append(Environment.NewLine).Append(enemyInfo);
                        }
                    }
                    else
                    {
                        sb.Append(Environment.NewLine).Append("- No creatures encountered");
                    }

                    sb.Append($"{Environment.NewLine}DAMAGE TAKEN:");
                    if (currentPlanetDamageLogs.Count > 0)
                    {
                        foreach (string damageInfo in currentPlanetDamageLogs)
                        {
                            sb.Append(Environment.NewLine).Append(damageInfo);
                        }
                    }
                    else
                    {
                        sb.Append($"{Environment.NewLine}- No damage taken");
                    }

                    sb.Append($"{Environment.NewLine}DEATHS ({currentPlanetDeathLogs.Count}):");
                    if (currentPlanetDeathLogs.Count > 0)
                    {
                        foreach (string deathInfo in currentPlanetDeathLogs)
                        {
                            sb.Append(Environment.NewLine).Append(deathInfo);
                        }
                    }
                    else
                    {
                        sb.Append($"{Environment.NewLine}- No deaths");
                    }

                    sb.Append($"{Environment.NewLine}FIGHTS:");
                    if (currentPlanetFightLogs.Count > 0)
                    {
                        foreach (string fightInfo in currentPlanetFightLogs)
                        {
                            sb.Append(Environment.NewLine).Append(fightInfo);
                        }
                    }
                    else
                    {
                        sb.Append($"{Environment.NewLine}- No fights occurred");
                    }

                    sb.Append($"{Environment.NewLine}GLOBAL:{Environment.NewLine}----------------------------------");

                    AppendRankedSection(sb, "ROUTING INFO", globalRoutes, totalRoutes, "route rate");
                    AppendRankedSection(sb, "WEATHER INFO", globalWeathers, totalWeathers, "weather rate");
                    AppendRankedSection(sb, "SCRAP INFO", globalScrapPicks, totalScrapPicks, "pick rate");
                    AppendRankedSection(sb, "SHOP INFO", globalShopPurchases, totalShopPurchases, "buy rate");
                    AppendRankedSection(sb, "CREATURES INFO", globalCreaturesSpawned, totalSpawnedCreatures, "spawn rate");
                    AppendRankedSection(sb, "DAMAGE INFO", globalDamageLogs, totalDamageEvents, "damage rate");
                    AppendRankedSection(sb, "DEATHS INFO", globalDeaths, totalDeathEvents, "death rate");
                    AppendRankedSection(sb, "FIGHTS INFO", globalFights, totalHitEvents, "hit rate");

                    sb.Append(Environment.NewLine);

                    File.AppendAllText(logFilePath, sb.ToString());
                    hasLoggedFirstEntry = true;

                    SaveGlobalStats();

                    ResetScrapList();
                }
            }
            catch (Exception) { }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "GrabObjectServerRpc")]
    public class LALoggerGrabObjectPatch
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerControllerB __instance, NetworkObjectReference grabbedObject)
        {
            if (grabbedObject.TryGet(out NetworkObject networkObject))
            {
                if (networkObject != null)
                {
                    GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
                    if (component != null)
                    {
                        if (component.playerHeldBy == null) component.playerHeldBy = __instance;

                        LA_Logger.LogPickedUpScrap(component);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "DamagePlayer")]
    public class LALoggerPlayerDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerControllerB __instance, int damageNumber, CauseOfDeath causeOfDeath, bool fallDamage)
        {
            try
            {
                if (__instance == null || !__instance.isPlayerControlled) return;

                int actualDamage = damageNumber;
                if (actualDamage > __instance.health)
                {
                    actualDamage = __instance.health;
                }

                string sourceName = "Unknown";
                string enemyName = "None";

                if (fallDamage)
                {
                    sourceName = "Fall Damage";
                }
                else if (causeOfDeath != CauseOfDeath.Unknown)
                {
                    sourceName = causeOfDeath.ToString();
                }

                bool isMeleeCause = causeOfDeath == CauseOfDeath.Mauling || causeOfDeath == CauseOfDeath.Bludgeoning ||
                                     causeOfDeath == CauseOfDeath.Strangulation || causeOfDeath == CauseOfDeath.Crushing;

                if (isMeleeCause)
                {
                    EnemyAI closestEnemy = LA_Logger.FindClosestEnemy(__instance.transform.position, 5f);
                    if (closestEnemy != null && closestEnemy.enemyType != null)
                    {
                        enemyName = LA_Logger.GetEnemyDisplayName(closestEnemy);
                    }
                }

                LA_Logger.LogPlayerDamage(__instance.playerUsername, actualDamage, sourceName, enemyName);

                if (causeOfDeath == CauseOfDeath.Bludgeoning && !__instance.isPlayerDead)
                {
                    PlayerControllerB attacker = FindClosestAttacker(__instance, 3.5f);
                    if (attacker != null)
                    {
                        int duration = LA_Logger.UpdateAndGetFightDuration(attacker.playerUsername, __instance.playerUsername);
                        int hpLeft = __instance.health - actualDamage;
                        string remainingHP = hpLeft <= 0 ? "0 (Dead)" : hpLeft.ToString();

                        LA_Logger.LogPlayerAttack(attacker.playerUsername, actualDamage, __instance.playerUsername, duration, remainingHP);
                    }
                }
            }
            catch (Exception) { }
        }

        private static PlayerControllerB FindClosestAttacker(PlayerControllerB victim, float maxDistance)
        {
            PlayerControllerB[] allPlayers = StartOfRound.Instance?.allPlayerScripts;
            if (allPlayers == null) return null;

            foreach (var p in allPlayers)
            {
                if (p != null && p.isPlayerControlled && !p.isPlayerDead && p != victim)
                {
                    if (Vector3.Distance(p.transform.position, victim.transform.position) < maxDistance)
                    {
                        return p;
                    }
                }
            }

            return null;
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "KillPlayer")]
    public class LALoggerPlayerKillPatch
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerControllerB __instance, CauseOfDeath causeOfDeath)
        {
            try
            {
                if (__instance == null || !__instance.isPlayerControlled || __instance.isPlayerDead) return;

                string deathTime = "00:00 AM";
                if (HUDManager.Instance != null && HUDManager.Instance.clockNumber != null)
                {
                    deathTime = HUDManager.Instance.clockNumber.text.Replace("\n", " ").Trim();
                }

                string location = __instance.isInsideFactory ? "Inside" : "Outside";

                string causeText = causeOfDeath.ToString();
                if (causeOfDeath == CauseOfDeath.Drowning || __instance.isUnderwater)
                {
                    causeText = "Underwater";
                }

                if (causeOfDeath == CauseOfDeath.Mauling || causeOfDeath == CauseOfDeath.Bludgeoning ||
    causeOfDeath == CauseOfDeath.Strangulation || causeOfDeath == CauseOfDeath.Crushing)
                {
                    EnemyAI closestEnemy = LA_Logger.FindClosestEnemy(__instance.transform.position, 8f);

                    if (closestEnemy != null && closestEnemy.enemyType != null)
                    {
                        string enemyName = LA_Logger.GetEnemyDisplayName(closestEnemy);
                        causeText = $"{causeText} - {enemyName}";
                    }
                }

                LA_Logger.LogPlayerDeath(__instance.playerUsername, deathTime, location, causeText);
            }
            catch (Exception) { }
        }
    }

    [HarmonyPatch(typeof(Terminal), "LoadNewNode")]
    public class LALoggerTerminalShopPatch
    {
        private static int previousCartCount = 0;
        private static int creditsBeforeConfirm = 0;

        [HarmonyPrefix]
        public static void Prefix(Terminal __instance, TerminalNode node)
        {
            try
            {
                if (__instance.orderedItemsFromTerminal == null) return;

                if (node != null && node.isConfirmationNode)
                {
                    previousCartCount = __instance.orderedItemsFromTerminal.Count;
                    creditsBeforeConfirm = __instance.groupCredits; return;
                }

                if (__instance.currentNode != null && __instance.currentNode.isConfirmationNode)
                {
                    int currentCount = __instance.orderedItemsFromTerminal.Count;

                    if (currentCount > previousCartCount)
                    {
                        int exactSpentCredits = creditsBeforeConfirm - __instance.groupCredits;

                        if (exactSpentCredits > 0)
                        {
                            int itemsCount = currentCount - previousCartCount;

                            int exactCostPerItem = exactSpentCredits / Mathf.Max(1, itemsCount);
                            int runningCredits = creditsBeforeConfirm;

                            for (int i = previousCartCount; i < currentCount; i++)
                            {
                                int itemIndex = __instance.orderedItemsFromTerminal[i];
                                if (__instance.buyableItemsList != null && itemIndex >= 0 && itemIndex < __instance.buyableItemsList.Length)
                                {
                                    Item shopItem = __instance.buyableItemsList[itemIndex];
                                    if (shopItem != null)
                                    {
                                        string name = shopItem.itemName;
                                        int creditsAfter = runningCredits - exactCostPerItem;
                                        if (creditsAfter < 0) creditsAfter = 0;

                                        LA_Logger.LogShopPurchase(name, exactCostPerItem, runningCredits, creditsAfter);
                                        runningCredits = creditsAfter;
                                    }
                                }
                            }
                        }
                    }
                    previousCartCount = currentCount;
                }
            }
            catch (Exception) { }
        }
    }

    [HarmonyPatch(typeof(EnemyAI), "HitEnemy")]
    public class LALoggerHitEnemyPatch
    {
        [HarmonyPrefix]
        public static void Prefix(EnemyAI __instance, int force, PlayerControllerB playerWhoHit)
        {
            try
            {
                if (__instance == null || playerWhoHit == null || __instance.isEnemyDead) return;

                string targetMonsterName = LA_Logger.GetEnemyDisplayName(__instance);

                int duration = LA_Logger.UpdateAndGetFightDuration(playerWhoHit.playerUsername, targetMonsterName);

                int hpLeft = __instance.enemyHP - force;
                string remainingHP = hpLeft <= 0 ? "0 (Dead)" : hpLeft.ToString();

                LA_Logger.LogPlayerAttack(playerWhoHit.playerUsername, force, targetMonsterName, duration, remainingHP);
            }
            catch (Exception) { }
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
}