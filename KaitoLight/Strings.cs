using System.Globalization;
using System.Windows.Forms;

namespace KaitoLight
{
    /// <summary>All interface text in Arabic (formal) and English. Language is chosen once at startup.</summary>
    internal static class L
    {
        public static bool Ar { get; private set; } = true;

        /// <summary>"ar", "en", or empty to follow the Windows display language.</summary>
        public static void Init(string lang)
        {
            if (lang == "ar") Ar = true;
            else if (lang == "en") Ar = false;
            else Ar = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
        }

        private static string T(string ar, string en) => Ar ? ar : en;

        public static RightToLeft Rtl => Ar ? RightToLeft.Yes : RightToLeft.No;

        /// <summary>Text flags aligned to the reading direction.</summary>
        public static TextFormatFlags Align => Ar ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left;

        // header
        public static string Subtitle => T("أداة لتشغيل الألعاب في وضع ملء الشاشة دون إطار", "Run your games in borderless fullscreen");
        public static string ServiceOn => T("الخدمة مفعّلة", "Service is on");
        public static string ServiceOff => T("الخدمة متوقفة", "Service is off");
        public static string ServiceOnHint => T("يُحوَّل كل تطبيق ألعاب يُشغَّل إلى وضع ملء الشاشة دون إطار تلقائيًا.",
            "Every game you launch is switched to borderless fullscreen automatically.");
        public static string ServiceOffHint => T("لن تُعدَّل نوافذ الألعاب حتى تُعاد الخدمة.", "Game windows are left alone until the service is turned back on.");

        // tabs / list
        public static string TabRunning => T("قيد التشغيل", "Running");
        public static string TabAll => T("كل الألعاب", "All games");
        public static string EmptyRunning => T("لا توجد ألعاب قيد التشغيل", "No games running");
        public static string EmptyRunningHint => T("شغّل أي لعبة، وستظهر هنا وتُحوَّل إلى ملء الشاشة دون إطار تلقائيًا.",
            "Start any game and it will appear here and switch to borderless fullscreen automatically.");
        public static string EmptyPausedHint => T("فعّل الخدمة من الأعلى لاستئناف تحويل الألعاب.", "Turn the service on above to resume.");
        public static string EmptyAll => T("لا توجد ألعاب محفوظة بعد", "No saved games yet");
        public static string EmptyAllHint => T("ستُحفظ هنا كل لعبة يتعرّف عليها البرنامج، لتتحكّم في إعداداتها وتشغّلها مباشرة.",
            "Every game KaitoLight recognises is saved here, so you can change its setting or launch it directly.");

        // card states
        public static string StateBorderless => T("يعمل بملء الشاشة دون إطار", "Borderless fullscreen");
        public static string StateWaiting => T("بانتظار ظهور نافذة اللعبة", "Waiting for the game window");
        public static string StateOffGame => T("التحويل معطّل لهذه اللعبة", "Turned off for this game");
        public static string StateNotRunning => T("غير قيد التشغيل — انقر للتشغيل", "Not running — click to launch");
        public static string CardTip => T("انقر للانتقال إلى اللعبة", "Click to switch to the game");
        public static string CardTipLaunch => T("انقر لتشغيل اللعبة", "Click to launch the game");
        public static string RemoveFromList => T("إزالة من القائمة", "Remove from list");
        public static string OpenFolder => T("فتح مجلد اللعبة", "Open game folder");

        // footer
        public static string AddGame => T("إضافة لعبة يدويًا", "Add a game manually");
        public static string StartWithWindows => T("التشغيل مع بدء Windows", "Start with Windows");
        public static string OtherLanguage => T("English", "العربية");

        // tray
        public static string TrayOn => T("●  الخدمة مفعّلة", "●  Service is on");
        public static string TrayOff => T("○  الخدمة متوقفة", "○  Service is off");
        public static string TrayPause => T("إيقاف الخدمة", "Turn off");
        public static string TrayResume => T("تفعيل الخدمة", "Turn on");
        public static string TrayOpen => T("فتح نافذة البرنامج", "Open window");
        public static string TrayExit => T("إنهاء البرنامج", "Exit");
        public static string PausedToast => T("توقفت الخدمة، وأُعيدت الألعاب إلى وضعها الأصلي.", "Service off. Games were restored to their original windows.");
        public static string ResumedToast => T("فُعّلت الخدمة.", "Service on.");
        public static string StartedTitle => T("يعمل KaitoLight الآن", "KaitoLight is running");
        public static string StartedBody => T("البرنامج في منطقة الإشعارات بجوار الساعة. انقر على أيقونته لإيقاف الخدمة أو تفعيلها.",
            "It lives in the notification area next to the clock. Click its icon to turn the service on or off.");
        public static string BackgroundTitle => T("يعمل KaitoLight في الخلفية", "KaitoLight is still running");
        public static string BackgroundBody => T("يستمر البرنامج في تحويل الألعاب. يمكنك الوصول إليه من منطقة الإشعارات، ولإنهائه اختر «إنهاء البرنامج» من قائمة الأيقونة.",
            "It keeps converting your games. Find it in the notification area; choose Exit from its menu to quit.");

        // picker
        public static string PickTitle => T("إضافة لعبة", "Add a game");
        public static string PickHint => T("تأكّد من تشغيل اللعبة، ثم اخترها من القائمة.\nسيتذكّرها البرنامج ويحوّلها تلقائيًا في كل مرة تُشغَّل فيها.",
            "Make sure the game is running, then pick it from the list.\nKaitoLight will remember it and convert it every time.");
        public static string PickNone => T("لا توجد نوافذ مفتوحة حاليًا.\nشغّل اللعبة أولًا، ثم أعد فتح هذه النافذة.",
            "No open windows found.\nStart the game first, then open this again.");
        public static string PickAdd => T("إضافة", "Add");
        public static string Cancel => T("إلغاء", "Cancel");

        // misc
        public static string AlreadyRunning => T("البرنامج يعمل بالفعل. يمكنك الوصول إليه من منطقة الإشعارات بجوار الساعة.",
            "KaitoLight is already running. Find it in the notification area next to the clock.");
        public static string RestartForLanguage => T("سيُعاد تشغيل البرنامج لتطبيق اللغة.", "KaitoLight will restart to apply the language.");

        public static string Count(string label, int n) => n == 0 ? label : $"{label} ({n})";
    }
}
