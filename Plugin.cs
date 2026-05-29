using BepInEx;
using HarmonyLib;
using System;
using LA_Changeloger;

[BepInPlugin("com.shroom1x.LAchangeloger", "LA_Changeloger", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    private readonly Harmony harmony = new Harmony("com.shroom1x.LAchangeloger");

    void Awake()
    {
        harmony.PatchAll();
        LA_Logger.Initialize();
        Logger.LogInfo("LA_Changeloger loaded and patches applied!");
    }
}
