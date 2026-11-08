# KaitoLight Fullscreen Fix 🎮

خلّ أي لعبة تشتغل **ملء الشاشة بلا حدود** (Borderless Fullscreen) بضغطة وحدة، وتقدر تطلع منها بـ Alt+Tab بسرعة وبدون وميض أو شاشة سوداء.

## ⬇️ التحميل والتشغيل

1. نزّل **KaitoLight-FullscreenFix.zip** من صفحة [Releases](https://github.com/exelight7/kaitolightfullsceenfix/releases/latest).
2. فك الضغط في أي مكان.
3. افتح `BorderlessGaming.exe` ووافق على صلاحية المسؤول.
4. شغّل لعبتك بوضع **Windowed** (نافذة)، وبتطلع في شريط **"ألعابك"**. اضغط عليها وخلاص ✔

> يحتاج .NET Framework 4.8، وهو موجود أصلاً في ويندوز 10 و 11.

## ✨ المميزات

- **يعرف ألعابك تلقائياً**: Steam و Epic و GOG و Riot و Ubisoft و EA و Xbox، وألعاب Unity و Unreal.
- **بطاقات ملوّنة بضغطة وحدة**: اضغط مرة تصير اللعبة ملء الشاشة، واضغط مرة ثانية ترجع لشكلها.
- **⚡ وضع تلقائي**: فعّله وأي لعبة تفتحها تصير بلا حدود بنفسها.
- **واجهة داكنة ملوّنة**، والألعاب تطلع أول القائمة.
- **يشتغل من أي مجلد** بدون تثبيت.
- كل خيارات Borderless Gaming الأصلية موجودة: المفضلة، اختيار الشاشة، إخفاء شريط المهام والماوس، والاختصارات.

## 🛠️ البناء من الكود

يحتاج Visual Studio 2022 أو Build Tools مع حزمة .NET Framework 4.8:

```
msbuild BorderlessGaming.sln /restore /p:Configuration=Release
```

الناتج يكون في `BorderlessGaming\bin\Release\`.

## 📜 الترخيص

مبني على [Borderless Gaming](https://github.com/Codeusa/Borderless-Gaming) من Codeusa، ومرخّص بـ GPLv2. التفاصيل في ملف [LICENSE](LICENSE).
