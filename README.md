# ⚡ KaitoLight

يحوّل ألعابك **Borderless ملء الشاشة** تلقائياً، وبدون ما تضغط شي.
تقدر تطلع من اللعبة بـ Alt+Tab على طول، بدون وميض ولا شاشة سوداء.

## ⬇️ التحميل والتشغيل

1. نزّل **KaitoLight-v2.1.zip** من صفحة [Releases](https://github.com/exelight7/kaitolightfullsceenfix/releases/latest).
2. فك الضغط، وافتح `KaitoLight.exe`، ووافق على صلاحية المسؤول.
3. البرنامج يشتغل من أيقونته جنب الساعة. شغّل أي لعبة، وبتصير ملء الشاشة بلا حدود بنفسها 🎮

> يحتاج .NET Framework 4.8، وهو موجود أصلاً في ويندوز 10 و 11.
>
> ⚠️ البرنامج غير موقّع رقمياً. إذا كانت ميزة **Smart App Control** شغّالة عندك، بيمنعه ويندوز من التشغيل.

## ✨ المميزات

- **تلقائي وغصب**: أي لعبة تفتحها تتحوّل Borderless. ولو رجّعت اللعبة الإطار، يحوّلها من جديد.
- **يعرف ألعابك**: Steam و Epic و GOG و Riot و Ubisoft و EA و Xbox و Battle.net و Rockstar، وألعاب Unity و Unreal و Godot و GameMaker و RPG Maker.
- **من أيقونة جنب الساعة**: ضغطة وحدة عليها وتوقّف الخدمة أو تشغّلها، والأيقونة تصير رمادية إذا كانت موقوفة.
- **واجهة هادئة**: ألوان مريحة للعين، ونصوص بالعربية الفصحى، وبطاقة لكل لعبة فيها مفتاح خاص.
- **لعبتك ما طلعت؟** اضغط "إضافة لعبة يدويًا" واخترها، وبيتذكرها كل مرة.
- **يشتغل بالخلفية** جنب الساعة، وفيه خيار يخليه يشتغل مع ويندوز.
- **ملف واحد** حجمه 42 كيلوبايت، بدون تثبيت ولا مكتبات زيادة.
- إذا طلعت منه، يرجّع الألعاب لشكلها الأصلي.

## 🛠️ البناء من الكود

يحتاج Visual Studio 2022 أو Build Tools مع حزمة .NET Framework 4.8:

```
msbuild KaitoLight\KaitoLight.csproj /p:Configuration=Release
```

الناتج يكون في `KaitoLight\bin\Release\KaitoLight.exe`.

مجلد `BorderlessGaming` فيه الإصدار القديم v1.0، وهو مبني على Borderless Gaming.

## 📜 الترخيص

GPLv2. الإصدار v1.0 مبني على [Borderless Gaming](https://github.com/Codeusa/Borderless-Gaming) من Codeusa. التفاصيل في ملف [LICENSE](LICENSE).
