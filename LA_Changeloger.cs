using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LA_Changeloger
{
    public static class ModInfo
    {
        private static string cachedVersion;

        private const string GithubUser = "shroom1x";
        private const string GithubRepo = "LETHAL-ADVENTURE";

        public static string GetVersion()
        {
            if (string.IsNullOrEmpty(cachedVersion))
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                cachedVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            }
            return cachedVersion;
        }

        public static async Task<string> DownloadChangelogAsync()
        {
            string url = $"https://raw.githubusercontent.com/{GithubUser}/{GithubRepo}/main/changelog.txt";
            try
            {
                using (WebClient client = new WebClient())
                {
                    string rawText = await client.DownloadStringTaskAsync(new Uri(url));

                    return $"Version {GetVersion()}:\n\n{rawText}";
                }
            }
            catch (Exception)
            {
                return $"Version {GetVersion()}:\n\n• Не удалось загрузить список изменений с GitHub. Проверьте подключение к интернету.";
            }
        }
    }

    [HarmonyPatch]
    public class LA_VersionTextPatch
    {
        [HarmonyPatch(typeof(MenuManager), "Start")]
        [HarmonyPostfix]
        private static void MenuManager_Start_Postfix(MenuManager __instance)
        {
            try
            {
                TextMeshProUGUI[] allTexts = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>(true);

                foreach (var tmpText in allTexts)
                {
                    if (tmpText != null && tmpText.gameObject.name == "VersionNum")
                    {
                        string newVersion = $"LETHAL ADVENTURE v{ModInfo.GetVersion()}";
                        tmpText.text = newVersion;
                        tmpText.SetText(newVersion);
                        tmpText.autoSizeTextContainer = true;
                        break;
                    }
                }
            }
            catch (Exception) { }
        }
    }

    [HarmonyPatch(typeof(MenuManager))]
    public class MainMenuButtonPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void RenameCreditsButton(MenuManager __instance)
        {
            Transform menuCanvas = __instance.transform.parent != null ? __instance.transform.parent : __instance.transform;
            if (menuCanvas == null) return;

            Transform creditsButtonTransform = menuCanvas.Find("MenuContainer/MainButtons/Credits");
            Transform creditsPanelTransform = menuCanvas.Find("MenuContainer/CreditsPanel");

            if (creditsButtonTransform == null || creditsPanelTransform == null) return;

            TextMeshProUGUI buttonText = creditsButtonTransform.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = "> CHANGELOG";
            }

            Transform newTextTransform = menuCanvas.Find("MenuContainer/LobbyHostSettings/FilesPanel/File3/Text (TMP) (3)");
            if (newTextTransform != null)
            {
                newTextTransform.SetParent(creditsButtonTransform, false);

                TextMeshProUGUI notifyText = newTextTransform.GetComponent<TextMeshProUGUI>();
                if (notifyText != null)
                {
                    notifyText.enabled = true;
                    newTextTransform.gameObject.SetActive(true);
                    notifyText.text = "NEW!";
                    notifyText.SetText("NEW!");
                    notifyText.autoSizeTextContainer = true;
                    notifyText.color = Color.green;

                    RectTransform rect = newTextTransform.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchorMin = new Vector2(1f, 0.5f);
                        rect.anchorMax = new Vector2(1f, 0.5f);
                        rect.pivot = new Vector2(0f, 0.5f);
                        rect.anchoredPosition = new Vector2(-160f, 0f);
                        rect.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    }

                    var oldPulsator = newTextTransform.gameObject.GetComponent<UIPulsator>();
                    if (oldPulsator != null) UnityEngine.Object.Destroy(oldPulsator);

                    var pulsator = newTextTransform.gameObject.AddComponent<UIPulsator>();
                    pulsator.pulseSpeed = 5f;
                    pulsator.pulseRange = 0.12f;
                }
            }

            Button buttonComponent = creditsButtonTransform.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.RemoveAllListeners();

                buttonComponent.onClick.AddListener(async () =>
{
    __instance.PlayConfirmSFX();

    Transform activeNotification = creditsButtonTransform.Find("Text (TMP) (3)");
    if (activeNotification != null)
    {
        activeNotification.gameObject.SetActive(false);
    }

    TextMeshProUGUI[] allTexts = creditsPanelTransform.GetComponentsInChildren<TextMeshProUGUI>(true);

    string onlineChangelog = await ModInfo.DownloadChangelogAsync();

    foreach (TextMeshProUGUI txt in allTexts)
    {
        if (txt.gameObject.name.Contains("CreditsText") || txt.text == "test")
        {
            txt.fontSize = 12.2f;
            txt.characterSpacing = -12;
            txt.lineSpacing = 40;

            txt.text = onlineChangelog;

            ScrollRect scrollRect = txt.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.scrollSensitivity = 0.025f;
            }
        }
        else if (txt.text == "Credits" || txt.gameObject.name.Contains("Title") || txt.gameObject.name.Contains("Header"))
        {
            txt.text = "LETHAL ADVENTURE (MODPACK)";
            txt.fontSize = 22.6f;
            txt.autoSizeTextContainer = true;
        }
    }

    creditsPanelTransform.gameObject.SetActive(true);
});
            }
        }
    }

    public class UIPulsator : MonoBehaviour
    {
        public float pulseSpeed = 4f;
        public float pulseRange = 0.15f;
        private Vector3 baseScale;
        private RectTransform rectTransform;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            baseScale = rectTransform != null ? rectTransform.localScale : transform.localScale;
        }

        void Update()
        {
            float scaleFactor = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseRange;
            if (rectTransform != null) rectTransform.localScale = baseScale * scaleFactor;
            else transform.localScale = baseScale * scaleFactor;
        }
    }
}