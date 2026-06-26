using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace PK2Encryptor;

public sealed partial class MainForm : Form
{
    private sealed class LanguageProfile
    {
        public LanguageProfile(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }

        public string Code { get; }
        public string DisplayName { get; }
    }

    private sealed class AppSettings
    {
        public string ThemeName { get; set; } = "Light Premium / Ivory Blue";
        public string LanguageCode { get; set; } = "en";
        public string BlowfishKey { get; set; } = "169841";
    }

    private static readonly LanguageProfile[] LanguageProfiles =
    {
        new("en", "English"),
        new("ar", "العربية"),
        new("tr", "Türkçe"),
        new("fr", "Français"),
        new("es", "Español"),
        new("de", "Deutsch"),
        new("pl", "Polski"),
        new("hu", "Magyar"),
        new("nl", "Nederlands"),
        new("el", "Ελληνικά"),
        new("pt-BR", "Português (Brasil)"),
        new("hi", "हिन्दी"),
        new("ko", "한국어"),
        new("zh-Hans", "简体中文"),
        new("zh-Hant", "繁體中文"),
        new("th", "ไทย"),
        new("fa", "فارسی")
    };

    private readonly ComboBox _languageBox = new ModernComboBox();
    private readonly Dictionary<Control, string> _localizedControls = new();
    private readonly Dictionary<TextBox, string> _localizedPlaceholders = new();
    private LanguageProfile _language = LanguageProfiles[0];
    private bool _applyingLanguage;
    private AppSettings _settings = new();

    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Setting.json");

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = CreateTranslations();

    private static Dictionary<string, Dictionary<string, string>> CreateTranslations()
    {
        var data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        void Add(string languageCode, params (string Key, string Value)[] values)
        {
            if(!data.TryGetValue(languageCode, out var map))
            {
                map = new Dictionary<string, string>(StringComparer.Ordinal);
                data[languageCode] = map;
            }
            foreach(var (key, value) in values)
            {
                map[key] = value;
            }
        }

        Add("en",
            ("PK2 Tools", "PK2 Tools"),
            ("Studio", "Studio"),
            ("PK2 Tools - Studio", "PK2 Tools - Studio"),
            ("Build • Extract • Inject • Secure", "Build • Extract • Inject • Secure"),
            ("WORKSPACE", "WORKSPACE"),
            ("Encryptor", "Encryptor"),
            ("Extractor", "Extractor"),
            ("Import / Inject", "Import / Inject"),
            ("Build PK2", "Build PK2"),
            ("Folder mode", "Folder mode"),
            ("PK2 file mode", "PK2 file mode"),
            ("INTERFACE THEME", "INTERFACE THEME"),
            ("LANGUAGE", "LANGUAGE"),
            ("Light Premium / Ivory Blue", "Light Premium / Ivory Blue"),
            ("Dark Premium / Obsidian Gold", "Dark Premium / Obsidian Gold"),
            ("GFX Compatible", "GFX Compatible"),
            ("Payload Secure", "Payload Secure"),
            ("GFX Compatible\r\nPayload Secure      ●\r\nv1.0.0", "GFX Compatible\r\nPayload Secure      ●\r\nv1.0.0"),
            ("PK2 workflow\r\n• Build PK2 archives\r\n• Extract PK2 files\r\n• Import and secure payloads", "PK2 workflow\r\n• Build PK2 archives\r\n• Extract PK2 files\r\n• Import and secure payloads"),
            ("Protect folders or internal PK2 payloads without changing the readable PK2 header.", "Protect folders or internal PK2 payloads without changing the readable PK2 header."),
            ("Mode\r\nFolder or PK2", "Mode\r\nFolder or PK2"),
            ("Output\r\nIn-place secure payload", "Output\r\nIn-place secure payload"),
            ("Encryption target list", "Encryption target list"),
            ("Folder path", "Folder path"),
            ("Paste or browse the folder path...", "Paste or browse the folder path..."),
            ("Browse", "Browse"),
            ("Include subfolders", "Include subfolders"),
            ("Include hidden/system files", "Include hidden/system files"),
            ("Explorer preview opens the current folder instantly. Include subfolders affects the Start operation, not the preview navigation.", "Explorer preview opens the current folder instantly. Include subfolders affects the Start operation, not the preview navigation."),
            ("PK2 file path", "PK2 file path"),
            ("Paste or browse a .pk2 file path...", "Paste or browse a .pk2 file path..."),
            ("PK2 file mode encrypts or decrypts stored payloads inside the selected archive. Import and Extractor have their own workspaces.", "PK2 file mode encrypts or decrypts stored payloads inside the selected archive. Import and Extractor have their own workspaces."),
            ("Name", "Name"),
            ("Size", "Size"),
            ("Type", "Type"),
            ("State", "State"),
            ("Operation", "Operation"),
            ("Encrypt", "Encrypt"),
            ("Decrypt", "Decrypt"),
            ("Choose a source, review the target list, then start the protected payload operation.", "Choose a source, review the target list, then start the protected payload operation."),
            ("Start", "Start"),
            ("Cancel", "Cancel"),
            ("LIVE STATUS", "LIVE STATUS"),
            ("Choose a valid folder.", "Choose a valid folder."),
            ("Status", "Status"),
            ("Ready.", "Ready."),
            ("Each page has its own controls. Select Encryptor, Extractor, Import, or Builder from the sidebar.", "Each page has its own controls. Select Encryptor, Extractor, Import, or Builder from the sidebar."),
            ("Read the internal file tree, restore payloads, or export raw stored bytes.", "Read the internal file tree, restore payloads, or export raw stored bytes."),
            ("Read\r\nPK2 contents", "Read\r\nPK2 contents"),
            ("Extract\r\nSelected or all", "Extract\r\nSelected or all"),
            ("PK2 file", "PK2 file"),
            ("Select the PK2 file to read...", "Select the PK2 file to read..."),
            ("Browse PK2", "Browse PK2"),
            ("Output folder", "Output folder"),
            ("Select where the extracted files will be written...", "Select where the extracted files will be written..."),
            ("Browse Output", "Browse Output"),
            ("Output file state", "Output file state"),
            ("Extract restored/plain files", "Extract restored/plain files"),
            ("Extract raw stored payload files", "Extract raw stored payload files"),
            ("Plain output restores custom payload encryption; raw output keeps bytes exactly as stored.", "Plain output restores custom payload encryption; raw output keeps bytes exactly as stored."),
            ("PK2 internal files", "PK2 internal files"),
            ("Select one or more internal files, or extract the complete archive.", "Select one or more internal files, or extract the complete archive."),
            ("Extract Selected", "Extract Selected"),
            ("Extract All", "Extract All"),
            ("Clear", "Clear"),
            ("Import / Injection", "Import / Injection"),
            ("Inject update folders into the client archive and keep payload mode consistent for GFXFileManager.", "Inject update folders into the client archive and keep payload mode consistent for GFXFileManager."),
            ("Target\r\nExisting PK2", "Target\r\nExisting PK2"),
            ("Mode\r\nPlain or encrypted", "Mode\r\nPlain or encrypted"),
            ("Target PK2 file", "Target PK2 file"),
            ("Select the client .pk2 file to inject into...", "Select the client .pk2 file to inject into..."),
            ("Import source folder", "Import source folder"),
            ("Select the folder that contains the files to inject...", "Select the folder that contains the files to inject..."),
            ("Browse Folder", "Browse Folder"),
            ("Stored payload state", "Stored payload state"),
            ("Store internal payloads plain", "Store internal payloads plain"),
            ("Store internal payloads encrypted", "Store internal payloads encrypted"),
            ("Encrypted mode is readable only through the matching GFXFileManager.dll.", "Encrypted mode is readable only through the matching GFXFileManager.dll."),
            ("Choose Target PK2, choose Import source folder, select payload state, then Import.", "Choose Target PK2, choose Import source folder, select payload state, then Import."),
            ("Import", "Import"),
            ("Import notes\r\n\r\nThe selected archive is updated in-place. Choose a folder such as Data, Media, Map, Music, Particles, or a folder containing those folders. Plain/encrypted payload mode is applied consistently to the whole PK2 so GFXFileManager.dll can read it correctly.", "Import notes\r\n\r\nThe selected archive is updated in-place. Choose a folder such as Data, Media, Map, Music, Particles, or a folder containing those folders. Plain/encrypted payload mode is applied consistently to the whole PK2 so GFXFileManager.dll can read it correctly."),
            ("Create Data.pk2, Media.pk2, Map.pk2, Music.pk2, or Particles.pk2 from source folders.", "Create Data.pk2, Media.pk2, Map.pk2, Music.pk2, or Particles.pk2 from source folders."),
            ("Queue\r\nKnown client folders", "Queue\r\nKnown client folders"),
            ("Build\r\nGFX compatible", "Build\r\nGFX compatible"),
            ("Source client/folder", "Source client/folder"),
            ("Select client root, or one folder such as Media/Data/Map...", "Select client root, or one folder such as Media/Data/Map..."),
            ("Browse Source", "Browse Source"),
            ("Select where Data.pk2 / Media.pk2 / Map.pk2 will be created...", "Select where Data.pk2 / Media.pk2 / Map.pk2 will be created..."),
            ("Builder PK2 queue", "Builder PK2 queue"),
            ("PK2 archive", "PK2 archive"),
            ("Source folder", "Source folder"),
            ("Output file", "Output file"),
            ("Encrypt directory entries", "Encrypt directory entries"),
            ("Encrypt internal payloads", "Encrypt internal payloads"),
            ("Encrypted payloads are decrypted by GFXFileManager.dll at runtime.", "Encrypted payloads are decrypted by GFXFileManager.dll at runtime."),
            ("Refresh", "Refresh"),
            ("Build selected", "Build selected"),
            ("Ready", "Ready"),
            ("Missing folder", "Missing folder"),
            ("Queued", "Queued"),
            ("Processing", "Processing"),
            ("Done", "Done"),
            ("Failed", "Failed"),
            ("Cancelled", "Cancelled"),
            ("Already encrypted", "Already encrypted"),
            ("Not encrypted", "Not encrypted"),
            ("File", "File"),
            ("Folder", "Folder"),
            ("Root", "Root"),
            ("Open", "Open"),
            ("Please select a valid folder.", "Please select a valid folder."),
            ("No files were found in the selected folder.", "No files were found in the selected folder."),
            ("Please select a valid PK2 file.", "Please select a valid PK2 file."),
            ("The selected file is not .pk2. Continue anyway?", "The selected file is not .pk2. Continue anyway?"),
            ("Please select a valid target PK2 file.", "Please select a valid target PK2 file."),
            ("The selected target file is not .pk2. Continue anyway?", "The selected target file is not .pk2. Continue anyway?"),
            ("Please select a valid import source folder.", "Please select a valid import source folder."),
            ("Please select a valid PK2 file to extract.", "Please select a valid PK2 file to extract."),
            ("Please select an output folder.", "Please select an output folder."),
            ("Please select one or more PK2 files from the extractor list.", "Please select one or more PK2 files from the extractor list."),
            ("Select at least one source folder to build.", "Select at least one source folder to build."),
            ("Select folder to encrypt or decrypt in-place", "Select folder to encrypt or decrypt in-place"),
            ("Select PK2 file", "Select PK2 file"),
            ("Select PK2 file to extract", "Select PK2 file to extract"),
            ("Select folder for extracted PK2 files", "Select folder for extracted PK2 files"),
            ("Select PK2 file to import into", "Select PK2 file to import into"),
            ("Select file to import into PK2", "Select file to import into PK2"),
            ("Select source folder to inject into the selected client PK2", "Select source folder to inject into the selected client PK2"),
            ("Select client root or a single PK2 source folder", "Select client root or a single PK2 source folder"),
            ("Select output folder for built PK2 archives", "Select output folder for built PK2 archives"),
            ("Operation completed successfully.", "Operation completed successfully."),
            ("Builder PK2 completed successfully.", "Builder PK2 completed successfully."),
            ("Folder import completed.", "Folder import completed."),
            ("PK2 extraction completed.", "PK2 extraction completed."),
            ("BLOWFISH KEY", "BLOWFISH KEY"),
            ("Default: 169841", "Default: 169841"),
            ("PK2 Blowfish Key", "PK2 Blowfish Key"),
            ("Changing this key affects Encryptor, Extractor, Import, and Builder.", "Changing this key affects Encryptor, Extractor, Import, and Builder."),
            ("Encrypted payload", "Encrypted payload"),
            ("Plain payload", "Plain payload"),
            ("Folder preview uses Explorer mode: open folders instantly, then press Start to process the selected root.", "Folder preview uses Explorer mode: open folders instantly, then press Start to process the selected root."),
            ("Opening folder view...", "Opening folder view..."),
            ("Folder view failed.", "Folder view failed."),
            ("Could not show this folder.", "Could not show this folder."),
            ("Preparing operation list...", "Preparing operation list..."),
            ("Scanning files in background...", "Scanning files in background..."),
            ("Operation list prepared. Preview remains in Explorer mode so navigation stays instant.", "Operation list prepared. Preview remains in Explorer mode so navigation stays instant."),
            ("Scan failed.", "Scan failed."),
            ("Could not prepare operation list.", "Could not prepare operation list."),
            ("PK2 mode keeps the archive path and filename unchanged.", "PK2 mode keeps the archive path and filename unchanged."),
            ("Cached PK2 listing is being used. Use Browse again or refresh after rebuilding the archive.", "Cached PK2 listing is being used. Use Browse again or refresh after rebuilding the archive."),
            ("Reading PK2 contents...", "Reading PK2 contents..."),
            ("Loading internal PK2 file tree...", "Loading internal PK2 file tree..."),
            ("Encrypting PK2 internal file payloads...", "Encrypting PK2 internal file payloads..."),
            ("Decrypting PK2 internal file payloads...", "Decrypting PK2 internal file payloads..."),
            ("PK2 operation completed.", "PK2 operation completed."),
            ("PK2 operation cancelled.", "PK2 operation cancelled."),
            ("The operation stopped before completion.", "The operation stopped before completion."),
            ("Operation failed.", "Operation failed."),
            ("PK2 operation failed.", "PK2 operation failed."),
            ("Build failed.", "Build failed."),
            ("Builder PK2 cancelled.", "Builder PK2 cancelled."),
            ("Builder PK2 completed.", "Builder PK2 completed."),
            ("Builder PK2 failed.", "Builder PK2 failed."),
            ("Building selected PK2 archives...", "Building selected PK2 archives..."),
            ("Choose a readable PK2 file.", "Choose a readable PK2 file."),
            ("Choose a valid PK2 file for extraction.", "Choose a valid PK2 file for extraction."),
            ("Choose a valid PK2 file.", "Choose a valid PK2 file."),
            ("Could not read PK2 contents.", "Could not read PK2 contents."),
            ("Double-click folders to browse; select files to extract or use Extract All.", "Double-click folders to browse; select files to extract or use Extract All."),
            ("Extraction failed.", "Extraction failed."),
            ("Extraction stopped before all selected files were written.", "Extraction stopped before all selected files were written."),
            ("Extractor writes files under the selected output folder.", "Extractor writes files under the selected output folder."),
            ("Import failed.", "Import failed."),
            ("Importing folder into PK2...", "Importing folder into PK2..."),
            ("Missing source folder: ", "Missing source folder: "),
            ("PK2 extraction cancelled.", "PK2 extraction cancelled."),
            ("PK2 extraction failed.", "PK2 extraction failed."),
            ("PK2 import cancelled.", "PK2 import cancelled."),
            ("PK2 import failed.", "PK2 import failed."),
            ("Preparing selected files...", "Preparing selected files..."),
            ("Reading PK2 contents for extraction...", "Reading PK2 contents for extraction..."),
            ("Skipped: Media\\type.txt stays plain", "Skipped: Media\\type.txt stays plain"),
            ("Some files failed. Check the Status column.", "Some files failed. Check the Status column."),
            ("Starting", "Starting"),
            ("Starting builder queue.", "Starting builder queue."),
            ("The active build stopped before the full queue completed.", "The active build stopped before the full queue completed."),
            ("The current file is never cancelled in the middle of writing to avoid partial encryption.", "The current file is never cancelled in the middle of writing to avoid partial encryption."),
            ("The import was stopped before completion.", "The import was stopped before completion."),
            ("The selected PK2 could not be listed. Details are shown in Status.", "The selected PK2 could not be listed. Details are shown in Status."),
            ("Up", "Up"),
            ("all listed PK2 files", "all listed PK2 files"),
            ("done", "done"),
            ("encrypted internal file payloads", "encrypted internal file payloads"),
            ("encrypted/GFXFileManager-readable", "encrypted/GFXFileManager-readable"),
            ("plain", "plain"),
            ("plain internal file payloads", "plain internal file payloads"),
            ("raw stored", "raw stored"),
            ("restored/plain", "restored/plain"),
            ("standard readable header + encrypted directory entries", "standard readable header + encrypted directory entries"),
            ("standard readable header + plain directory entries", "standard readable header + plain directory entries"),
            ("the client PK2 root", "the client PK2 root"));

        Add("ar",
            ("PK2 Tools", "أدوات PK2"), ("Studio", "استوديو"), ("PK2 Tools - Studio", "PK2 Tools - Studio"), ("Build • Extract • Inject • Secure", "بناء • استخراج • حقن • حماية"), ("WORKSPACE", "مساحة العمل"), ("Encryptor", "التشفير"), ("Extractor", "الاستخراج"), ("Import / Inject", "استيراد / حقن"), ("Build PK2", "بناء PK2"), ("Folder mode", "وضع المجلد"), ("PK2 file mode", "وضع ملف PK2"), ("INTERFACE THEME", "ثيم الواجهة"), ("LANGUAGE", "اللغة"), ("Light Premium / Ivory Blue", "فاتح احترافي / أزرق عاجي"), ("Dark Premium / Obsidian Gold", "داكن احترافي / ذهبي"), ("GFX Compatible", "متوافق مع GFX"), ("Payload Secure", "Payload مؤمّن"), ("Folder path", "مسار المجلد"), ("Paste or browse the folder path...", "الصق أو اختر مسار المجلد..."), ("Browse", "استعراض"), ("Include subfolders", "تضمين المجلدات الفرعية"), ("Include hidden/system files", "تضمين الملفات المخفية/النظام"), ("PK2 file path", "مسار ملف PK2"), ("Paste or browse a .pk2 file path...", "الصق أو اختر مسار ملف .pk2..."), ("Name", "الاسم"), ("Size", "الحجم"), ("Type", "النوع"), ("State", "الحالة"), ("Operation", "العملية"), ("Encrypt", "تشفير"), ("Decrypt", "فك التشفير"), ("Start", "بدء"), ("Cancel", "إلغاء"), ("LIVE STATUS", "الحالة الحالية"), ("Choose a valid folder.", "اختر مجلدًا صحيحًا."), ("Status", "الحالة"), ("Ready.", "جاهز."), ("Ready", "جاهز"), ("Missing folder", "المجلد مفقود"), ("Queued", "في الانتظار"), ("Processing", "جاري المعالجة"), ("Done", "تم"), ("Failed", "فشل"), ("Cancelled", "تم الإلغاء"), ("Already encrypted", "مشفر بالفعل"), ("Not encrypted", "غير مشفر"), ("File", "ملف"), ("Folder", "مجلد"), ("Root", "الجذر"), ("Open", "فتح"), ("PK2 file", "ملف PK2"), ("Browse PK2", "استعراض PK2"), ("Output folder", "مجلد الإخراج"), ("Browse Output", "استعراض الإخراج"), ("Output file state", "حالة ملفات الإخراج"), ("Extract restored/plain files", "استخراج ملفات مستعادة/عادية"), ("Extract raw stored payload files", "استخراج payload الخام المخزن"), ("PK2 internal files", "ملفات PK2 الداخلية"), ("Extract Selected", "استخراج المحدد"), ("Extract All", "استخراج الكل"), ("Clear", "مسح"), ("Target PK2 file", "ملف PK2 الهدف"), ("Import source folder", "مجلد مصدر الاستيراد"), ("Browse Folder", "استعراض مجلد"), ("Stored payload state", "حالة payload المخزن"), ("Store internal payloads plain", "تخزين payload الداخلي عادي"), ("Store internal payloads encrypted", "تخزين payload الداخلي مشفر"), ("Import", "استيراد"), ("Source client/folder", "العميل/المجلد المصدر"), ("Browse Source", "استعراض المصدر"), ("Builder PK2 queue", "قائمة بناء PK2"), ("PK2 archive", "أرشيف PK2"), ("Source folder", "مجلد المصدر"), ("Output file", "ملف الإخراج"), ("Encrypt directory entries", "تشفير إدخالات الدليل"), ("Encrypt internal payloads", "تشفير payload الداخلي"), ("Refresh", "تحديث"), ("Build selected", "بناء المحدد"), ("Please select a valid folder.", "اختر مجلدًا صحيحًا."), ("Please select a valid PK2 file.", "اختر ملف PK2 صحيحًا."), ("Please select an output folder.", "اختر مجلد إخراج."), ("Operation completed successfully.", "اكتملت العملية بنجاح."), ("Builder PK2 completed successfully.", "تم بناء PK2 بنجاح."), ("Folder import completed.", "تم استيراد المجلد."), ("PK2 extraction completed.", "تم استخراج PK2."), ("BLOWFISH KEY", "مفتاح BLOWFISH"), ("Default: 169841", "الافتراضي: 169841"), ("PK2 Blowfish Key", "مفتاح Blowfish للـ PK2"), ("Changing this key affects Encryptor, Extractor, Import, and Builder.", "تغيير هذا المفتاح يؤثر على التشفير والاستخراج والاستيراد والبناء."), ("Encrypted payload", "Payload مشفر"), ("Plain payload", "Payload عادي"), ("Folder preview uses Explorer mode: open folders instantly, then press Start to process the selected root.", "معاينة المجلد تستخدم وضع المستكشف؛ افتح المجلدات فورًا ثم اضغط بدء لمعالجة الجذر المحدد."), ("Opening folder view...", "فتح عرض المجلد..."), ("Folder view failed.", "فشل عرض المجلد."), ("Could not show this folder.", "تعذر عرض هذا المجلد."), ("Preparing operation list...", "تجهيز قائمة العمليات..."), ("Scanning files in background...", "فحص الملفات في الخلفية..."), ("Operation list prepared. Preview remains in Explorer mode so navigation stays instant.", "تم تجهيز قائمة العمليات. المعاينة تبقى في وضع المستكشف لتظل سريعة."), ("Scan failed.", "فشل الفحص."), ("Could not prepare operation list.", "تعذر تجهيز قائمة العمليات."), ("PK2 mode keeps the archive path and filename unchanged.", "وضع PK2 يحافظ على مسار واسم الأرشيف كما هما."), ("Cached PK2 listing is being used. Use Browse again or refresh after rebuilding the archive.", "يتم استخدام قائمة PK2 المحفوظة. استخدم الاستعراض أو التحديث بعد إعادة البناء."), ("Reading PK2 contents...", "قراءة محتويات PK2..."), ("Loading internal PK2 file tree...", "تحميل شجرة ملفات PK2 الداخلية..."), ("Encrypting PK2 internal file payloads...", "تشفير Payload ملفات PK2 الداخلية..."), ("Decrypting PK2 internal file payloads...", "فك تشفير Payload ملفات PK2 الداخلية..."), ("PK2 operation completed.", "اكتملت عملية PK2."), ("PK2 operation cancelled.", "تم إلغاء عملية PK2."), ("The operation stopped before completion.", "توقفت العملية قبل الاكتمال."), ("Operation failed.", "فشلت العملية."), ("PK2 operation failed.", "فشلت عملية PK2."), ("Build failed.", "فشل البناء."), ("Builder PK2 cancelled.", "تم إلغاء بناء PK2."), ("Builder PK2 completed.", "اكتمل بناء PK2."), ("Builder PK2 failed.", "فشل بناء PK2."), ("Building selected PK2 archives...", "جاري بناء أرشيفات PK2 المحددة..."), ("Choose a readable PK2 file.", "اختر ملف PK2 قابلًا للقراءة."), ("Choose a valid PK2 file for extraction.", "اختر ملف PK2 صحيحًا للاستخراج."), ("Choose a valid PK2 file.", "اختر ملف PK2 صحيحًا."), ("Could not read PK2 contents.", "تعذرت قراءة محتويات PK2."), ("Double-click folders to browse; select files to extract or use Extract All.", "اضغط مرتين على المجلدات للتصفح؛ حدد الملفات للاستخراج أو استخدم استخراج الكل."), ("Extraction failed.", "فشل الاستخراج."), ("Extraction stopped before all selected files were written.", "توقف الاستخراج قبل كتابة كل الملفات المحددة."), ("Extractor writes files under the selected output folder.", "الاستخراج يكتب الملفات داخل مجلد الإخراج المحدد."), ("Import failed.", "فشل الاستيراد."), ("Importing folder into PK2...", "جاري حقن المجلد داخل PK2..."), ("Missing source folder: ", "مجلد المصدر مفقود: "), ("PK2 extraction cancelled.", "تم إلغاء استخراج PK2."), ("PK2 extraction failed.", "فشل استخراج PK2."), ("PK2 import cancelled.", "تم إلغاء استيراد PK2."), ("PK2 import failed.", "فشل استيراد PK2."), ("Preparing selected files...", "تجهيز الملفات المحددة..."), ("Reading PK2 contents for extraction...", "قراءة محتويات PK2 للاستخراج..."), ("Skipped: Media\\type.txt stays plain", "تم التخطي: Media\\type.txt يبقى بدون تشفير"), ("Some files failed. Check the Status column.", "فشلت بعض الملفات. راجع عمود الحالة."), ("Starting", "بدء"), ("Starting builder queue.", "بدء قائمة البناء."), ("The active build stopped before the full queue completed.", "توقف البناء الحالي قبل اكتمال القائمة."), ("The current file is never cancelled in the middle of writing to avoid partial encryption.", "لا يتم إلغاء الملف أثناء الكتابة لتجنب التشفير الجزئي."), ("The import was stopped before completion.", "توقف الاستيراد قبل الاكتمال."), ("The selected PK2 could not be listed. Details are shown in Status.", "تعذر عرض محتويات ملف PK2 المحدد. التفاصيل في الحالة."), ("Up", "أعلى"), ("all listed PK2 files", "كل ملفات PK2 المعروضة"), ("done", "تم"), ("encrypted internal file payloads", "Payload داخلي مشفر"), ("encrypted/GFXFileManager-readable", "مشفر/قابل للقراءة عبر GFXFileManager"), ("plain", "عادي"), ("plain internal file payloads", "Payload داخلي عادي"), ("raw stored", "خام مخزن"), ("restored/plain", "مستعاد/عادي"), ("standard readable header + encrypted directory entries", "هيدر قياسي مقروء + إدخالات دليل مشفرة"), ("standard readable header + plain directory entries", "هيدر قياسي مقروء + إدخالات دليل عادية"), ("the client PK2 root", "جذر PK2 الخاص بالعميل"));

        Add("tr",
            ("PK2 Tools", "PK2 Araçları"), ("Studio", "Stüdyo"), ("PK2 Tools - Studio", "PK2 Tools - Studio"), ("Build • Extract • Inject • Secure", "Oluştur • Çıkar • Enjekte et • Koru"), ("WORKSPACE", "ÇALIŞMA ALANI"), ("Encryptor", "Şifreleyici"), ("Extractor", "Çıkarıcı"), ("Import / Inject", "İçe Aktar / Enjekte Et"), ("Build PK2", "PK2 Oluştur"), ("Folder mode", "Klasör modu"), ("PK2 file mode", "PK2 dosya modu"), ("INTERFACE THEME", "ARAYÜZ TEMASI"), ("LANGUAGE", "DİL"), ("Light Premium / Ivory Blue", "Açık Premium / Fildişi Mavi"), ("Dark Premium / Obsidian Gold", "Koyu Premium / Obsidyen Altın"), ("Browse", "Gözat"), ("Start", "Başlat"), ("Cancel", "İptal"), ("Clear", "Temizle"), ("Encrypt", "Şifrele"), ("Decrypt", "Şifre çöz"), ("Ready", "Hazır"), ("Ready.", "Hazır."), ("Processing", "İşleniyor"), ("Done", "Tamam"), ("Failed", "Başarısız"), ("Cancelled", "İptal edildi"), ("Name", "Ad"), ("Size", "Boyut"), ("Type", "Tür"), ("State", "Durum"), ("Status", "Durum"), ("Folder path", "Klasör yolu"), ("PK2 file path", "PK2 dosya yolu"), ("Output folder", "Çıkış klasörü"), ("Extract Selected", "Seçileni Çıkar"), ("Extract All", "Tümünü Çıkar"), ("Import", "İçe aktar"), ("Refresh", "Yenile"), ("Build selected", "Seçileni oluştur"));

        Add("fr", ("PK2 Tools", "Outils PK2"), ("Studio", "Studio"), ("WORKSPACE", "ESPACE DE TRAVAIL"), ("Encryptor", "Chiffrement"), ("Extractor", "Extraction"), ("Import / Inject", "Importer / Injecter"), ("Build PK2", "Créer PK2"), ("Folder mode", "Mode dossier"), ("PK2 file mode", "Mode fichier PK2"), ("INTERFACE THEME", "THÈME"), ("LANGUAGE", "LANGUE"), ("Browse", "Parcourir"), ("Start", "Démarrer"), ("Cancel", "Annuler"), ("Clear", "Effacer"), ("Encrypt", "Chiffrer"), ("Decrypt", "Déchiffrer"), ("Ready", "Prêt"), ("Ready.", "Prêt."), ("Processing", "Traitement"), ("Done", "Terminé"), ("Failed", "Échec"), ("Cancelled", "Annulé"), ("Name", "Nom"), ("Size", "Taille"), ("Type", "Type"), ("State", "État"), ("Status", "Statut"), ("Folder path", "Chemin du dossier"), ("PK2 file path", "Chemin du fichier PK2"), ("Output folder", "Dossier de sortie"), ("Extract Selected", "Extraire la sélection"), ("Extract All", "Tout extraire"), ("Import", "Importer"), ("Refresh", "Actualiser"), ("Build selected", "Créer la sélection"));
        Add("es", ("PK2 Tools", "Herramientas PK2"), ("Studio", "Estudio"), ("WORKSPACE", "ÁREA DE TRABAJO"), ("Encryptor", "Cifrador"), ("Extractor", "Extractor"), ("Import / Inject", "Importar / Inyectar"), ("Build PK2", "Crear PK2"), ("Folder mode", "Modo carpeta"), ("PK2 file mode", "Modo archivo PK2"), ("INTERFACE THEME", "TEMA"), ("LANGUAGE", "IDIOMA"), ("Browse", "Examinar"), ("Start", "Iniciar"), ("Cancel", "Cancelar"), ("Clear", "Limpiar"), ("Encrypt", "Cifrar"), ("Decrypt", "Descifrar"), ("Ready", "Listo"), ("Ready.", "Listo."), ("Processing", "Procesando"), ("Done", "Hecho"), ("Failed", "Error"), ("Cancelled", "Cancelado"), ("Name", "Nombre"), ("Size", "Tamaño"), ("Type", "Tipo"), ("State", "Estado"), ("Status", "Estado"), ("Folder path", "Ruta de carpeta"), ("PK2 file path", "Ruta del archivo PK2"), ("Output folder", "Carpeta de salida"), ("Extract Selected", "Extraer selección"), ("Extract All", "Extraer todo"), ("Import", "Importar"), ("Refresh", "Actualizar"), ("Build selected", "Crear selección"));
        Add("de", ("PK2 Tools", "PK2-Werkzeuge"), ("Studio", "Studio"), ("WORKSPACE", "ARBEITSBEREICH"), ("Encryptor", "Verschlüsselung"), ("Extractor", "Extraktor"), ("Import / Inject", "Importieren / Injizieren"), ("Build PK2", "PK2 erstellen"), ("Folder mode", "Ordnermodus"), ("PK2 file mode", "PK2-Dateimodus"), ("INTERFACE THEME", "OBERFLÄCHEN-DESIGN"), ("LANGUAGE", "SPRACHE"), ("Browse", "Durchsuchen"), ("Start", "Start"), ("Cancel", "Abbrechen"), ("Clear", "Leeren"), ("Encrypt", "Verschlüsseln"), ("Decrypt", "Entschlüsseln"), ("Ready", "Bereit"), ("Ready.", "Bereit."), ("Processing", "Wird verarbeitet"), ("Done", "Fertig"), ("Failed", "Fehlgeschlagen"), ("Cancelled", "Abgebrochen"), ("Name", "Name"), ("Size", "Größe"), ("Type", "Typ"), ("State", "Status"), ("Status", "Status"), ("Folder path", "Ordnerpfad"), ("PK2 file path", "PK2-Dateipfad"), ("Output folder", "Ausgabeordner"), ("Extract Selected", "Auswahl extrahieren"), ("Extract All", "Alles extrahieren"), ("Import", "Importieren"), ("Refresh", "Aktualisieren"), ("Build selected", "Auswahl erstellen"));
        Add("pl", ("PK2 Tools", "Narzędzia PK2"), ("Studio", "Studio"), ("WORKSPACE", "OBSZAR ROBOCZY"), ("Encryptor", "Szyfrowanie"), ("Extractor", "Ekstraktor"), ("Import / Inject", "Import / Wstrzyknięcie"), ("Build PK2", "Buduj PK2"), ("Folder mode", "Tryb folderu"), ("PK2 file mode", "Tryb pliku PK2"), ("INTERFACE THEME", "MOTYW"), ("LANGUAGE", "JĘZYK"), ("Browse", "Przeglądaj"), ("Start", "Start"), ("Cancel", "Anuluj"), ("Clear", "Wyczyść"), ("Encrypt", "Szyfruj"), ("Decrypt", "Odszyfruj"), ("Ready", "Gotowe"), ("Ready.", "Gotowe."), ("Processing", "Przetwarzanie"), ("Done", "Ukończono"), ("Failed", "Błąd"), ("Cancelled", "Anulowano"), ("Name", "Nazwa"), ("Size", "Rozmiar"), ("Type", "Typ"), ("State", "Stan"), ("Status", "Status"), ("Folder path", "Ścieżka folderu"), ("PK2 file path", "Ścieżka pliku PK2"), ("Output folder", "Folder wyjściowy"), ("Extract Selected", "Wyodrębnij zaznaczone"), ("Extract All", "Wyodrębnij wszystko"), ("Import", "Importuj"), ("Refresh", "Odśwież"), ("Build selected", "Buduj zaznaczone"));
        Add("hu", ("PK2 Tools", "PK2 eszközök"), ("Studio", "Stúdió"), ("WORKSPACE", "MUNKATERÜLET"), ("Encryptor", "Titkosító"), ("Extractor", "Kicsomagoló"), ("Import / Inject", "Importálás / Befecskendezés"), ("Build PK2", "PK2 építése"), ("Folder mode", "Mappa mód"), ("PK2 file mode", "PK2 fájl mód"), ("INTERFACE THEME", "FELÜLETI TÉMA"), ("LANGUAGE", "NYELV"), ("Browse", "Tallózás"), ("Start", "Indítás"), ("Cancel", "Mégse"), ("Clear", "Törlés"), ("Encrypt", "Titkosítás"), ("Decrypt", "Visszafejtés"), ("Ready", "Kész"), ("Ready.", "Kész."), ("Processing", "Feldolgozás"), ("Done", "Kész"), ("Failed", "Sikertelen"), ("Cancelled", "Megszakítva"), ("Name", "Név"), ("Size", "Méret"), ("Type", "Típus"), ("State", "Állapot"), ("Status", "Állapot"), ("Folder path", "Mappa útvonala"), ("PK2 file path", "PK2 fájl útvonala"), ("Output folder", "Kimeneti mappa"), ("Extract Selected", "Kijelöltek kicsomagolása"), ("Extract All", "Összes kicsomagolása"), ("Import", "Importálás"), ("Refresh", "Frissítés"), ("Build selected", "Kijelöltek építése"));
        Add("nl", ("PK2 Tools", "PK2-hulpmiddelen"), ("Studio", "Studio"), ("WORKSPACE", "WERKRUIMTE"), ("Encryptor", "Versleuteling"), ("Extractor", "Extractor"), ("Import / Inject", "Importeren / Injecteren"), ("Build PK2", "PK2 bouwen"), ("Folder mode", "Mapmodus"), ("PK2 file mode", "PK2-bestandsmodus"), ("INTERFACE THEME", "THEMA"), ("LANGUAGE", "TAAL"), ("Browse", "Bladeren"), ("Start", "Start"), ("Cancel", "Annuleren"), ("Clear", "Wissen"), ("Encrypt", "Versleutelen"), ("Decrypt", "Ontsleutelen"), ("Ready", "Gereed"), ("Ready.", "Gereed."), ("Processing", "Verwerken"), ("Done", "Klaar"), ("Failed", "Mislukt"), ("Cancelled", "Geannuleerd"), ("Name", "Naam"), ("Size", "Grootte"), ("Type", "Type"), ("State", "Status"), ("Status", "Status"), ("Folder path", "Mappad"), ("PK2 file path", "PK2-bestandspad"), ("Output folder", "Uitvoermap"), ("Extract Selected", "Selectie uitpakken"), ("Extract All", "Alles uitpakken"), ("Import", "Importeren"), ("Refresh", "Vernieuwen"), ("Build selected", "Selectie bouwen"));
        Add("el", ("PK2 Tools", "Εργαλεία PK2"), ("Studio", "Στούντιο"), ("WORKSPACE", "ΧΩΡΟΣ ΕΡΓΑΣΙΑΣ"), ("Encryptor", "Κρυπτογράφηση"), ("Extractor", "Εξαγωγή"), ("Import / Inject", "Εισαγωγή / Έγχυση"), ("Build PK2", "Δημιουργία PK2"), ("Folder mode", "Λειτουργία φακέλου"), ("PK2 file mode", "Λειτουργία αρχείου PK2"), ("INTERFACE THEME", "ΘΕΜΑ"), ("LANGUAGE", "ΓΛΩΣΣΑ"), ("Browse", "Αναζήτηση"), ("Start", "Έναρξη"), ("Cancel", "Άκυρο"), ("Clear", "Καθαρισμός"), ("Encrypt", "Κρυπτογράφηση"), ("Decrypt", "Αποκρυπτογράφηση"), ("Ready", "Έτοιμο"), ("Ready.", "Έτοιμο."), ("Processing", "Επεξεργασία"), ("Done", "Ολοκληρώθηκε"), ("Failed", "Απέτυχε"), ("Cancelled", "Ακυρώθηκε"), ("Name", "Όνομα"), ("Size", "Μέγεθος"), ("Type", "Τύπος"), ("State", "Κατάσταση"), ("Status", "Κατάσταση"), ("Folder path", "Διαδρομή φακέλου"), ("PK2 file path", "Διαδρομή αρχείου PK2"), ("Output folder", "Φάκελος εξόδου"), ("Extract Selected", "Εξαγωγή επιλεγμένων"), ("Extract All", "Εξαγωγή όλων"), ("Import", "Εισαγωγή"), ("Refresh", "Ανανέωση"), ("Build selected", "Δημιουργία επιλεγμένων"));
        Add("pt-BR", ("PK2 Tools", "Ferramentas PK2"), ("Studio", "Estúdio"), ("WORKSPACE", "ÁREA DE TRABALHO"), ("Encryptor", "Criptografia"), ("Extractor", "Extrator"), ("Import / Inject", "Importar / Injetar"), ("Build PK2", "Criar PK2"), ("Folder mode", "Modo pasta"), ("PK2 file mode", "Modo arquivo PK2"), ("INTERFACE THEME", "TEMA"), ("LANGUAGE", "IDIOMA"), ("Browse", "Procurar"), ("Start", "Iniciar"), ("Cancel", "Cancelar"), ("Clear", "Limpar"), ("Encrypt", "Criptografar"), ("Decrypt", "Descriptografar"), ("Ready", "Pronto"), ("Ready.", "Pronto."), ("Processing", "Processando"), ("Done", "Concluído"), ("Failed", "Falhou"), ("Cancelled", "Cancelado"), ("Name", "Nome"), ("Size", "Tamanho"), ("Type", "Tipo"), ("State", "Estado"), ("Status", "Status"), ("Folder path", "Caminho da pasta"), ("PK2 file path", "Caminho do arquivo PK2"), ("Output folder", "Pasta de saída"), ("Extract Selected", "Extrair selecionados"), ("Extract All", "Extrair tudo"), ("Import", "Importar"), ("Refresh", "Atualizar"), ("Build selected", "Criar selecionados"));
        Add("hi", ("PK2 Tools", "PK2 टूल्स"), ("Studio", "स्टूडियो"), ("WORKSPACE", "वर्कस्पेस"), ("Encryptor", "एन्क्रिप्टर"), ("Extractor", "एक्सट्रैक्टर"), ("Import / Inject", "इम्पोर्ट / इंजेक्ट"), ("Build PK2", "PK2 बनाएं"), ("Folder mode", "फोल्डर मोड"), ("PK2 file mode", "PK2 फ़ाइल मोड"), ("INTERFACE THEME", "इंटरफ़ेस थीम"), ("LANGUAGE", "भाषा"), ("Browse", "ब्राउज़"), ("Start", "शुरू"), ("Cancel", "रद्द"), ("Clear", "साफ़"), ("Encrypt", "एन्क्रिप्ट"), ("Decrypt", "डिक्रिप्ट"), ("Ready", "तैयार"), ("Ready.", "तैयार."), ("Processing", "प्रोसेसिंग"), ("Done", "पूर्ण"), ("Failed", "विफल"), ("Cancelled", "रद्द हुआ"), ("Name", "नाम"), ("Size", "आकार"), ("Type", "प्रकार"), ("State", "स्थिति"), ("Status", "स्थिति"), ("Folder path", "फोल्डर पथ"), ("PK2 file path", "PK2 फ़ाइल पथ"), ("Output folder", "आउटपुट फोल्डर"), ("Extract Selected", "चयनित निकालें"), ("Extract All", "सब निकालें"), ("Import", "इम्पोर्ट"), ("Refresh", "रिफ्रेश"), ("Build selected", "चयनित बनाएं"));
        Add("ko", ("PK2 Tools", "PK2 도구"), ("Studio", "스튜디오"), ("WORKSPACE", "작업 공간"), ("Encryptor", "암호화"), ("Extractor", "추출"), ("Import / Inject", "가져오기 / 삽입"), ("Build PK2", "PK2 빌드"), ("Folder mode", "폴더 모드"), ("PK2 file mode", "PK2 파일 모드"), ("INTERFACE THEME", "인터페이스 테마"), ("LANGUAGE", "언어"), ("Browse", "찾아보기"), ("Start", "시작"), ("Cancel", "취소"), ("Clear", "지우기"), ("Encrypt", "암호화"), ("Decrypt", "복호화"), ("Ready", "준비됨"), ("Ready.", "준비됨."), ("Processing", "처리 중"), ("Done", "완료"), ("Failed", "실패"), ("Cancelled", "취소됨"), ("Name", "이름"), ("Size", "크기"), ("Type", "유형"), ("State", "상태"), ("Status", "상태"), ("Folder path", "폴더 경로"), ("PK2 file path", "PK2 파일 경로"), ("Output folder", "출력 폴더"), ("Extract Selected", "선택 추출"), ("Extract All", "모두 추출"), ("Import", "가져오기"), ("Refresh", "새로 고침"), ("Build selected", "선택 빌드"));
        Add("zh-Hans", ("PK2 Tools", "PK2 工具"), ("Studio", "工作室"), ("WORKSPACE", "工作区"), ("Encryptor", "加密器"), ("Extractor", "提取器"), ("Import / Inject", "导入 / 注入"), ("Build PK2", "构建 PK2"), ("Folder mode", "文件夹模式"), ("PK2 file mode", "PK2 文件模式"), ("INTERFACE THEME", "界面主题"), ("LANGUAGE", "语言"), ("Browse", "浏览"), ("Start", "开始"), ("Cancel", "取消"), ("Clear", "清除"), ("Encrypt", "加密"), ("Decrypt", "解密"), ("Ready", "就绪"), ("Ready.", "就绪。"), ("Processing", "处理中"), ("Done", "完成"), ("Failed", "失败"), ("Cancelled", "已取消"), ("Name", "名称"), ("Size", "大小"), ("Type", "类型"), ("State", "状态"), ("Status", "状态"), ("Folder path", "文件夹路径"), ("PK2 file path", "PK2 文件路径"), ("Output folder", "输出文件夹"), ("Extract Selected", "提取所选"), ("Extract All", "全部提取"), ("Import", "导入"), ("Refresh", "刷新"), ("Build selected", "构建所选"));
        Add("zh-Hant", ("PK2 Tools", "PK2 工具"), ("Studio", "工作室"), ("WORKSPACE", "工作區"), ("Encryptor", "加密器"), ("Extractor", "提取器"), ("Import / Inject", "匯入 / 注入"), ("Build PK2", "建置 PK2"), ("Folder mode", "資料夾模式"), ("PK2 file mode", "PK2 檔案模式"), ("INTERFACE THEME", "介面主題"), ("LANGUAGE", "語言"), ("Browse", "瀏覽"), ("Start", "開始"), ("Cancel", "取消"), ("Clear", "清除"), ("Encrypt", "加密"), ("Decrypt", "解密"), ("Ready", "就緒"), ("Ready.", "就緒。"), ("Processing", "處理中"), ("Done", "完成"), ("Failed", "失敗"), ("Cancelled", "已取消"), ("Name", "名稱"), ("Size", "大小"), ("Type", "類型"), ("State", "狀態"), ("Status", "狀態"), ("Folder path", "資料夾路徑"), ("PK2 file path", "PK2 檔案路徑"), ("Output folder", "輸出資料夾"), ("Extract Selected", "提取所選"), ("Extract All", "全部提取"), ("Import", "匯入"), ("Refresh", "重新整理"), ("Build selected", "建置所選"));
        Add("th", ("PK2 Tools", "เครื่องมือ PK2"), ("Studio", "สตูดิโอ"), ("WORKSPACE", "พื้นที่ทำงาน"), ("Encryptor", "ตัวเข้ารหัส"), ("Extractor", "ตัวแยกไฟล์"), ("Import / Inject", "นำเข้า / ใส่ข้อมูล"), ("Build PK2", "สร้าง PK2"), ("Folder mode", "โหมดโฟลเดอร์"), ("PK2 file mode", "โหมดไฟล์ PK2"), ("INTERFACE THEME", "ธีมอินเทอร์เฟซ"), ("LANGUAGE", "ภาษา"), ("Browse", "เรียกดู"), ("Start", "เริ่ม"), ("Cancel", "ยกเลิก"), ("Clear", "ล้าง"), ("Encrypt", "เข้ารหัส"), ("Decrypt", "ถอดรหัส"), ("Ready", "พร้อม"), ("Ready.", "พร้อม"), ("Processing", "กำลังประมวลผล"), ("Done", "เสร็จสิ้น"), ("Failed", "ล้มเหลว"), ("Cancelled", "ยกเลิกแล้ว"), ("Name", "ชื่อ"), ("Size", "ขนาด"), ("Type", "ประเภท"), ("State", "สถานะ"), ("Status", "สถานะ"), ("Folder path", "เส้นทางโฟลเดอร์"), ("PK2 file path", "เส้นทางไฟล์ PK2"), ("Output folder", "โฟลเดอร์ปลายทาง"), ("Extract Selected", "แยกที่เลือก"), ("Extract All", "แยกทั้งหมด"), ("Import", "นำเข้า"), ("Refresh", "รีเฟรช"), ("Build selected", "สร้างที่เลือก"));
        Add("fa", ("PK2 Tools", "ابزارهای PK2"), ("Studio", "استودیو"), ("WORKSPACE", "فضای کاری"), ("Encryptor", "رمزگذاری"), ("Extractor", "استخراج"), ("Import / Inject", "وارد کردن / تزریق"), ("Build PK2", "ساخت PK2"), ("Folder mode", "حالت پوشه"), ("PK2 file mode", "حالت فایل PK2"), ("INTERFACE THEME", "تم رابط"), ("LANGUAGE", "زبان"), ("Browse", "مرور"), ("Start", "شروع"), ("Cancel", "لغو"), ("Clear", "پاک کردن"), ("Encrypt", "رمزگذاری"), ("Decrypt", "رمزگشایی"), ("Ready", "آماده"), ("Ready.", "آماده."), ("Processing", "در حال پردازش"), ("Done", "انجام شد"), ("Failed", "ناموفق"), ("Cancelled", "لغو شد"), ("Name", "نام"), ("Size", "اندازه"), ("Type", "نوع"), ("State", "وضعیت"), ("Status", "وضعیت"), ("Folder path", "مسیر پوشه"), ("PK2 file path", "مسیر فایل PK2"), ("Output folder", "پوشه خروجی"), ("Extract Selected", "استخراج انتخاب‌شده"), ("Extract All", "استخراج همه"), ("Import", "وارد کردن"), ("Refresh", "تازه‌سازی"), ("Build selected", "ساخت انتخاب‌شده"));


        // Complete Traditional Chinese UI map: every visible label, button, placeholder,
        // list column, status, hint, and operation message is overridden here so the UI
        // does not fall back to English when 繁體中文 is selected.
        Add("zh-Hant",
            ("PK2 Tools", "PK2 工具"),
            ("Studio", "工作室"),
            ("PK2 Tools - Studio", "PK2 工具 - 工作室"),
            ("Build • Extract • Inject • Secure", "建置 • 提取 • 注入 • 保護"),
            ("WORKSPACE", "工作區"),
            ("Encryptor", "加密器"),
            ("Extractor", "提取器"),
            ("Import / Inject", "匯入 / 注入"),
            ("Build PK2", "建置 PK2"),
            ("Folder mode", "資料夾模式"),
            ("PK2 file mode", "PK2 檔案模式"),
            ("INTERFACE THEME", "介面主題"),
            ("LANGUAGE", "語言"),
            ("Light Premium / Ivory Blue", "淺色高級 / 象牙藍"),
            ("Dark Premium / Obsidian Gold", "深色高級 / 黑曜金"),
            ("GFX Compatible", "GFX 相容"),
            ("Payload Secure", "Payload 安全"),
            ("GFX Compatible\r\nPayload Secure      ●\r\nv1.0.0", "GFX 相容\r\nPayload 安全      ●\r\nv1.0.0"),
            ("PK2 workflow\r\n• Build PK2 archives\r\n• Extract PK2 files\r\n• Import and secure payloads", "PK2 工作流程\r\n• 建置 PK2 封包\r\n• 提取 PK2 檔案\r\n• 匯入並保護 Payload"),
            ("Protect folders or internal PK2 payloads without changing the readable PK2 header.", "保護資料夾或 PK2 內部 Payload，且不變更可讀的 PK2 標頭。"),
            ("Mode\r\nFolder or PK2", "模式\r\n資料夾或 PK2"),
            ("Output\r\nIn-place secure payload", "輸出\r\n原地安全 Payload"),
            ("Encryption target list", "加密目標清單"),
            ("Folder path", "資料夾路徑"),
            ("Paste or browse the folder path...", "貼上或瀏覽資料夾路徑..."),
            ("Browse", "瀏覽"),
            ("Include subfolders", "包含子資料夾"),
            ("Include hidden/system files", "包含隱藏/系統檔案"),
            ("Explorer preview opens the current folder instantly. Include subfolders affects the Start operation, not the preview navigation.", "檔案總管預覽會立即開啟目前資料夾。包含子資料夾只影響開始操作，不影響預覽導覽。"),
            ("PK2 file path", "PK2 檔案路徑"),
            ("Paste or browse a .pk2 file path...", "貼上或瀏覽 .pk2 檔案路徑..."),
            ("PK2 file mode encrypts or decrypts stored payloads inside the selected archive. Import and Extractor have their own workspaces.", "PK2 檔案模式會加密或解密所選封包內儲存的 Payload。匯入與提取器各有自己的工作區。"),
            ("Name", "名稱"),
            ("Size", "大小"),
            ("Type", "類型"),
            ("State", "狀態"),
            ("Operation", "操作"),
            ("Encrypt", "加密"),
            ("Decrypt", "解密"),
            ("Choose a source, review the target list, then start the protected payload operation.", "選擇來源、檢查目標清單，然後開始受保護的 Payload 操作。"),
            ("Start", "開始"),
            ("Cancel", "取消"),
            ("LIVE STATUS", "即時狀態"),
            ("Choose a valid folder.", "請選擇有效的資料夾。"),
            ("Status", "狀態"),
            ("Ready.", "就緒。"),
            ("Each page has its own controls. Select Encryptor, Extractor, Import, or Builder from the sidebar.", "每個頁面都有自己的控制項。請從側邊欄選擇加密器、提取器、匯入或建置器。"),
            ("Read the internal file tree, restore payloads, or export raw stored bytes.", "讀取內部檔案樹、還原 Payload，或匯出原始儲存位元組。"),
            ("Read\r\nPK2 contents", "讀取\r\nPK2 內容"),
            ("Extract\r\nSelected or all", "提取\r\n所選或全部"),
            ("PK2 file", "PK2 檔案"),
            ("Select the PK2 file to read...", "選擇要讀取的 PK2 檔案..."),
            ("Browse PK2", "瀏覽 PK2"),
            ("Output folder", "輸出資料夾"),
            ("Select where the extracted files will be written...", "選擇提取檔案的寫入位置..."),
            ("Browse Output", "瀏覽輸出"),
            ("Output file state", "輸出檔案狀態"),
            ("Extract restored/plain files", "提取已還原/普通檔案"),
            ("Extract raw stored payload files", "提取原始儲存 Payload 檔案"),
            ("Plain output restores custom payload encryption; raw output keeps bytes exactly as stored.", "普通輸出會還原自訂 Payload 加密；原始輸出會保持儲存位元組完全不變。"),
            ("PK2 internal files", "PK2 內部檔案"),
            ("Select one or more internal files, or extract the complete archive.", "選擇一個或多個內部檔案，或提取完整封包。"),
            ("Extract Selected", "提取所選"),
            ("Extract All", "全部提取"),
            ("Clear", "清除"),
            ("Import / Injection", "匯入 / 注入"),
            ("Inject update folders into the client archive and keep payload mode consistent for GFXFileManager.", "將更新資料夾注入客戶端封包，並保持 Payload 模式與 GFXFileManager 一致。"),
            ("Target\r\nExisting PK2", "目標\r\n現有 PK2"),
            ("Mode\r\nPlain or encrypted", "模式\r\n普通或加密"),
            ("Target PK2 file", "目標 PK2 檔案"),
            ("Select the client .pk2 file to inject into...", "選擇要注入的客戶端 .pk2 檔案..."),
            ("Import source folder", "匯入來源資料夾"),
            ("Select the folder that contains the files to inject...", "選擇包含要注入檔案的資料夾..."),
            ("Browse Folder", "瀏覽資料夾"),
            ("Stored payload state", "儲存 Payload 狀態"),
            ("Store internal payloads plain", "以普通模式儲存內部 Payload"),
            ("Store internal payloads encrypted", "以加密模式儲存內部 Payload"),
            ("Encrypted mode is readable only through the matching GFXFileManager.dll.", "加密模式只能透過相符的 GFXFileManager.dll 讀取。"),
            ("Choose Target PK2, choose Import source folder, select payload state, then Import.", "選擇目標 PK2、選擇匯入來源資料夾、選擇 Payload 狀態，然後匯入。"),
            ("Import", "匯入"),
            ("Import notes\r\n\r\nThe selected archive is updated in-place. Choose a folder such as Data, Media, Map, Music, Particles, or a folder containing those folders. Plain/encrypted payload mode is applied consistently to the whole PK2 so GFXFileManager.dll can read it correctly.", "匯入備註\r\n\r\n所選封包會原地更新。請選擇 Data、Media、Map、Music、Particles 等資料夾，或包含這些資料夾的資料夾。普通/加密 Payload 模式會一致套用到整個 PK2，確保 GFXFileManager.dll 能正確讀取。"),
            ("Create Data.pk2, Media.pk2, Map.pk2, Music.pk2, or Particles.pk2 from source folders.", "從來源資料夾建立 Data.pk2、Media.pk2、Map.pk2、Music.pk2 或 Particles.pk2。"),
            ("Queue\r\nKnown client folders", "佇列\r\n已知客戶端資料夾"),
            ("Build\r\nGFX compatible", "建置\r\nGFX 相容"),
            ("Source client/folder", "來源客戶端/資料夾"),
            ("Select client root, or one folder such as Media/Data/Map...", "選擇客戶端根目錄，或 Media/Data/Map 等單一資料夾..."),
            ("Browse Source", "瀏覽來源"),
            ("Select where Data.pk2 / Media.pk2 / Map.pk2 will be created...", "選擇 Data.pk2 / Media.pk2 / Map.pk2 的建立位置..."),
            ("Builder PK2 queue", "PK2 建置佇列"),
            ("PK2 archive", "PK2 封包"),
            ("Source folder", "來源資料夾"),
            ("Output file", "輸出檔案"),
            ("Encrypt directory entries", "加密目錄項目"),
            ("Encrypt internal payloads", "加密內部 Payload"),
            ("Encrypted payloads are decrypted by GFXFileManager.dll at runtime.", "加密 Payload 會在執行時由 GFXFileManager.dll 解密。"),
            ("Refresh", "重新整理"),
            ("Build selected", "建置所選"),
            ("Ready", "就緒"),
            ("Missing folder", "缺少資料夾"),
            ("Queued", "已排入佇列"),
            ("Processing", "處理中"),
            ("Done", "完成"),
            ("Failed", "失敗"),
            ("Cancelled", "已取消"),
            ("Already encrypted", "已加密"),
            ("Not encrypted", "未加密"),
            ("File", "檔案"),
            ("Folder", "資料夾"),
            ("Root", "根目錄"),
            ("Open", "開啟"),
            ("Please select a valid folder.", "請選擇有效的資料夾。"),
            ("No files were found in the selected folder.", "在所選資料夾中找不到檔案。"),
            ("Please select a valid PK2 file.", "請選擇有效的 PK2 檔案。"),
            ("The selected file is not .pk2. Continue anyway?", "所選檔案不是 .pk2。仍要繼續嗎？"),
            ("Please select a valid target PK2 file.", "請選擇有效的目標 PK2 檔案。"),
            ("The selected target file is not .pk2. Continue anyway?", "所選目標檔案不是 .pk2。仍要繼續嗎？"),
            ("Please select a valid import source folder.", "請選擇有效的匯入來源資料夾。"),
            ("Please select a valid PK2 file to extract.", "請選擇有效的 PK2 檔案進行提取。"),
            ("Please select an output folder.", "請選擇輸出資料夾。"),
            ("Please select one or more PK2 files from the extractor list.", "請從提取器清單中選擇一個或多個 PK2 檔案。"),
            ("Select at least one source folder to build.", "請至少選擇一個來源資料夾進行建置。"),
            ("Select folder to encrypt or decrypt in-place", "選擇要原地加密或解密的資料夾"),
            ("Select PK2 file", "選擇 PK2 檔案"),
            ("Select PK2 file to extract", "選擇要提取的 PK2 檔案"),
            ("Select folder for extracted PK2 files", "選擇提取 PK2 檔案的資料夾"),
            ("Select PK2 file to import into", "選擇要匯入的 PK2 檔案"),
            ("Select file to import into PK2", "選擇要匯入 PK2 的檔案"),
            ("Select source folder to inject into the selected client PK2", "選擇要注入所選客戶端 PK2 的來源資料夾"),
            ("Select client root or a single PK2 source folder", "選擇客戶端根目錄或單一 PK2 來源資料夾"),
            ("Select output folder for built PK2 archives", "選擇已建置 PK2 封包的輸出資料夾"),
            ("Operation completed successfully.", "操作已成功完成。"),
            ("Builder PK2 completed successfully.", "PK2 建置已成功完成。"),
            ("Folder import completed.", "資料夾匯入已完成。"),
            ("PK2 extraction completed.", "PK2 提取已完成。"),
            ("BLOWFISH KEY", "BLOWFISH 金鑰"),
            ("Default: 169841", "預設值：169841"),
            ("PK2 Blowfish Key", "PK2 Blowfish 金鑰"),
            ("Changing this key affects Encryptor, Extractor, Import, and Builder.", "變更此金鑰會影響加密器、提取器、匯入與建置器。"),
            ("Encrypted payload", "加密 Payload"),
            ("Plain payload", "普通 Payload"),
            ("Folder preview uses Explorer mode: open folders instantly, then press Start to process the selected root.", "資料夾預覽使用檔案總管模式：可立即開啟資料夾，然後按開始處理所選根目錄。"),
            ("Opening folder view...", "正在開啟資料夾檢視..."),
            ("Folder view failed.", "資料夾檢視失敗。"),
            ("Could not show this folder.", "無法顯示此資料夾。"),
            ("Preparing operation list...", "正在準備操作清單..."),
            ("Scanning files in background...", "正在背景掃描檔案..."),
            ("Operation list prepared. Preview remains in Explorer mode so navigation stays instant.", "操作清單已準備。預覽仍保持檔案總管模式，因此導覽會立即回應。"),
            ("Scan failed.", "掃描失敗。"),
            ("Could not prepare operation list.", "無法準備操作清單。"),
            ("PK2 mode keeps the archive path and filename unchanged.", "PK2 模式會保持封包路徑與檔名不變。"),
            ("Cached PK2 listing is being used. Use Browse again or refresh after rebuilding the archive.", "正在使用快取的 PK2 清單。重新建置封包後請再次瀏覽或重新整理。"),
            ("Reading PK2 contents...", "正在讀取 PK2 內容..."),
            ("Loading internal PK2 file tree...", "正在載入 PK2 內部檔案樹..."),
            ("Encrypting PK2 internal file payloads...", "正在加密 PK2 內部檔案 Payload..."),
            ("Decrypting PK2 internal file payloads...", "正在解密 PK2 內部檔案 Payload..."),
            ("PK2 operation completed.", "PK2 操作已完成。"),
            ("PK2 operation cancelled.", "PK2 操作已取消。"),
            ("The operation stopped before completion.", "操作在完成前已停止。"),
            ("Operation failed.", "操作失敗。"),
            ("PK2 operation failed.", "PK2 操作失敗。"),
            ("Build failed.", "建置失敗。"),
            ("Builder PK2 cancelled.", "PK2 建置已取消。"),
            ("Builder PK2 completed.", "PK2 建置已完成。"),
            ("Builder PK2 failed.", "PK2 建置失敗。"),
            ("Building selected PK2 archives...", "正在建置所選 PK2 封包..."),
            ("Choose a readable PK2 file.", "請選擇可讀取的 PK2 檔案。"),
            ("Choose a valid PK2 file for extraction.", "請選擇有效的 PK2 檔案進行提取。"),
            ("Choose a valid PK2 file.", "請選擇有效的 PK2 檔案。"),
            ("Could not read PK2 contents.", "無法讀取 PK2 內容。"),
            ("Double-click folders to browse; select files to extract or use Extract All.", "雙擊資料夾進行瀏覽；選擇檔案提取，或使用全部提取。"),
            ("Extraction failed.", "提取失敗。"),
            ("Extraction stopped before all selected files were written.", "提取在所有所選檔案寫入前已停止。"),
            ("Extractor writes files under the selected output folder.", "提取器會將檔案寫入所選輸出資料夾下。"),
            ("Import failed.", "匯入失敗。"),
            ("Importing folder into PK2...", "正在將資料夾匯入 PK2..."),
            ("Missing source folder: ", "缺少來源資料夾："),
            ("PK2 extraction cancelled.", "PK2 提取已取消。"),
            ("PK2 extraction failed.", "PK2 提取失敗。"),
            ("PK2 import cancelled.", "PK2 匯入已取消。"),
            ("PK2 import failed.", "PK2 匯入失敗。"),
            ("Preparing selected files...", "正在準備所選檔案..."),
            ("Reading PK2 contents for extraction...", "正在讀取 PK2 內容以進行提取..."),
            ("Skipped: Media\\type.txt stays plain", "已跳過：Media\\type.txt 保持普通未加密"),
            ("Some files failed. Check the Status column.", "部分檔案失敗。請檢查狀態欄。"),
            ("Starting", "正在開始"),
            ("Starting builder queue.", "正在開始建置佇列。"),
            ("The active build stopped before the full queue completed.", "目前建置在整個佇列完成前已停止。"),
            ("The current file is never cancelled in the middle of writing to avoid partial encryption.", "為避免部分加密，目前檔案在寫入中不會被取消。"),
            ("The import was stopped before completion.", "匯入在完成前已停止。"),
            ("The selected PK2 could not be listed. Details are shown in Status.", "無法列出所選 PK2。詳細資訊顯示在狀態中。"),
            ("Up", "上一層"),
            ("all listed PK2 files", "所有列出的 PK2 檔案"),
            ("done", "完成"),
            ("encrypted internal file payloads", "已加密內部檔案 Payload"),
            ("encrypted/GFXFileManager-readable", "已加密 / GFXFileManager 可讀"),
            ("plain", "普通"),
            ("plain internal file payloads", "普通內部檔案 Payload"),
            ("raw stored", "原始儲存"),
            ("restored/plain", "已還原/普通"),
            ("standard readable header + encrypted directory entries", "標準可讀標頭 + 已加密目錄項目"),
            ("standard readable header + plain directory entries", "標準可讀標頭 + 普通目錄項目"),
            ("the client PK2 root", "客戶端 PK2 根目錄"));

        // Complete Simplified Chinese UI map for the same full key set.
        Add("zh-Hans",
            ("PK2 Tools", "PK2 工具"),
            ("Studio", "工作室"),
            ("PK2 Tools - Studio", "PK2 工具 - 工作室"),
            ("Build • Extract • Inject • Secure", "构建 • 提取 • 注入 • 保护"),
            ("WORKSPACE", "工作区"),
            ("Encryptor", "加密器"),
            ("Extractor", "提取器"),
            ("Import / Inject", "导入 / 注入"),
            ("Build PK2", "构建 PK2"),
            ("Folder mode", "文件夹模式"),
            ("PK2 file mode", "PK2 文件模式"),
            ("INTERFACE THEME", "界面主题"),
            ("LANGUAGE", "语言"),
            ("Light Premium / Ivory Blue", "浅色高级 / 象牙蓝"),
            ("Dark Premium / Obsidian Gold", "深色高级 / 黑曜金"),
            ("GFX Compatible", "GFX 兼容"),
            ("Payload Secure", "Payload 安全"),
            ("Folder path", "文件夹路径"),
            ("Paste or browse the folder path...", "粘贴或浏览文件夹路径..."),
            ("Browse", "浏览"),
            ("Include subfolders", "包含子文件夹"),
            ("Include hidden/system files", "包含隐藏/系统文件"),
            ("PK2 file path", "PK2 文件路径"),
            ("Paste or browse a .pk2 file path...", "粘贴或浏览 .pk2 文件路径..."),
            ("Name", "名称"),
            ("Size", "大小"),
            ("Type", "类型"),
            ("State", "状态"),
            ("Operation", "操作"),
            ("Encrypt", "加密"),
            ("Decrypt", "解密"),
            ("Start", "开始"),
            ("Cancel", "取消"),
            ("LIVE STATUS", "实时状态"),
            ("Choose a valid folder.", "请选择有效文件夹。"),
            ("Status", "状态"),
            ("Ready.", "就绪。"),
            ("PK2 file", "PK2 文件"),
            ("Select the PK2 file to read...", "选择要读取的 PK2 文件..."),
            ("Browse PK2", "浏览 PK2"),
            ("Output folder", "输出文件夹"),
            ("Select where the extracted files will be written...", "选择提取文件的写入位置..."),
            ("Browse Output", "浏览输出"),
            ("Output file state", "输出文件状态"),
            ("Extract restored/plain files", "提取已还原/普通文件"),
            ("Extract raw stored payload files", "提取原始存储 Payload 文件"),
            ("PK2 internal files", "PK2 内部文件"),
            ("Extract Selected", "提取所选"),
            ("Extract All", "全部提取"),
            ("Clear", "清除"),
            ("Import / Injection", "导入 / 注入"),
            ("Target PK2 file", "目标 PK2 文件"),
            ("Import source folder", "导入源文件夹"),
            ("Browse Folder", "浏览文件夹"),
            ("Stored payload state", "存储 Payload 状态"),
            ("Store internal payloads plain", "以普通模式存储内部 Payload"),
            ("Store internal payloads encrypted", "以加密模式存储内部 Payload"),
            ("Import", "导入"),
            ("Source client/folder", "源客户端/文件夹"),
            ("Browse Source", "浏览源"),
            ("Builder PK2 queue", "PK2 构建队列"),
            ("PK2 archive", "PK2 封包"),
            ("Source folder", "源文件夹"),
            ("Output file", "输出文件"),
            ("Encrypt directory entries", "加密目录项"),
            ("Encrypt internal payloads", "加密内部 Payload"),
            ("Refresh", "刷新"),
            ("Build selected", "构建所选"),
            ("Ready", "就绪"),
            ("Missing folder", "缺少文件夹"),
            ("Queued", "已排队"),
            ("Processing", "处理中"),
            ("Done", "完成"),
            ("Failed", "失败"),
            ("Cancelled", "已取消"),
            ("Already encrypted", "已加密"),
            ("Not encrypted", "未加密"),
            ("File", "文件"),
            ("Folder", "文件夹"),
            ("Root", "根目录"),
            ("Open", "打开"),
            ("BLOWFISH KEY", "BLOWFISH 密钥"),
            ("Default: 169841", "默认值：169841"),
            ("PK2 Blowfish Key", "PK2 Blowfish 密钥"),
            ("Encrypted payload", "加密 Payload"),
            ("Plain payload", "普通 Payload"),
            ("Up", "上一层"),
            ("plain", "普通"),
            ("raw stored", "原始存储"),
            ("restored/plain", "已还原/普通"));


        Add("en",
            ("Parent folder", "Parent folder"),
            ("Folder view: {0} - {1:N0} folder(s), {2:N0} file(s).", "Folder view: {0} - {1:N0} folder(s), {2:N0} file(s)."),
            ("Prepared {0:N0} file(s), {1}.", "Prepared {0:N0} file(s), {1}."),
            ("PK2 cache loaded instantly: {0} - {1:N0} internal file(s).", "PK2 cache loaded instantly: {0} - {1:N0} internal file(s)."),
            ("PK2 selected: {0} - {1:N0} internal file(s), {2}.", "PK2 selected: {0} - {1:N0} internal file(s), {2}."),
            ("Extractor cache loaded instantly: {0} - {1:N0} internal file(s).", "Extractor cache loaded instantly: {0} - {1:N0} internal file(s)."),
            ("Extractor ready: {0} - {1:N0} internal file(s), {2}.", "Extractor ready: {0} - {1:N0} internal file(s), {2}."),
            ("No internal files were listed from this PK2.", "No internal files were listed from this PK2."),
            ("Double-click folders to browse internal PK2 contents like Windows Explorer.", "Double-click folders to browse internal PK2 contents like Windows Explorer."),
            ("No internal files were listed. Make sure the selected archive is a real Silkroad PK2 and GFXFileManager.dll is rebuilt from this source.", "No internal files were listed. Make sure the selected archive is a real Silkroad PK2 and GFXFileManager.dll is rebuilt from this source."),
            ("PK2 root folder.", "PK2 root folder."),
            ("PK2 folder: {0}", "PK2 folder: {0}"),
            ("This will {0} {1:N0} file(s) in the selected folder using the same names and paths. No backup files will be created. Continue?", "This will {0} {1:N0} file(s) in the selected folder using the same names and paths. No backup files will be created. Continue?"),
            ("This will {0} all internal file payloads inside the selected PK2 in-place. Encrypted payloads require the matching GFXFileManager.dll. The PK2 filename and path will not change. Continue?", "This will {0} all internal file payloads inside the selected PK2 in-place. Encrypted payloads require the matching GFXFileManager.dll. The PK2 filename and path will not change. Continue?"),
            ("the client PK2 folder '{0}'", "the client PK2 folder '{0}'"),
            ("This will inject the selected folder into {0}. Folder structure will be preserved and all PK2 payloads will be stored as {1} data. Continue?", "This will inject the selected folder into {0}. Folder structure will be preserved and all PK2 payloads will be stored as {1} data. Continue?"),
            ("{0:N0} selected PK2 file(s)", "{0:N0} selected PK2 file(s)"),
            ("This will extract {0} to the output folder and keep the internal PK2 folder layout. Output files will be {1}. Continue?", "This will extract {0} to the output folder and keep the internal PK2 folder layout. Output files will be {1}. Continue?"),
            ("This will build {0:N0} PK2 archive(s) with {1} and {2}. Encrypted payloads require the matching GFXFileManager.dll. Continue?", "This will build {0:N0} PK2 archive(s) with {1} and {2}. Encrypted payloads require the matching GFXFileManager.dll. Continue?"),
            ("Starting {0}...", "Starting {0}..."),
            ("{0}: {1:N0} / {2:N0}", "{0}: {1:N0} / {2:N0}"),
            ("Finished. {0:N0} file(s) processed, {1:N0} skipped.", "Finished. {0:N0} file(s) processed, {1:N0} skipped."),
            ("Finished with errors. {0:N0} processed, {1:N0} skipped, {2:N0} failed.", "Finished with errors. {0:N0} processed, {1:N0} skipped, {2:N0} failed."),
            ("Cancelled. {0:N0} file(s) completed before cancellation.", "Cancelled. {0:N0} file(s) completed before cancellation."),
            ("Building {0}.pk2 ({1:N0}/{2:N0})...", "Building {0}.pk2 ({1:N0}/{2:N0})..."),
            ("Building {0}.pk2 ({1:N0}/{2:N0})", "Building {0}.pk2 ({1:N0}/{2:N0})"),
            ("Building", "Building"),
            ("Finalizing", "Finalizing"),
            ("Selected PK2 archives were created with encrypted internal payloads readable through GFXFileManager.dll.", "Selected PK2 archives were created with encrypted internal payloads readable through GFXFileManager.dll."),
            ("Selected PK2 archives were created with plain internal payloads.", "Selected PK2 archives were created with plain internal payloads."),
            ("The selected PK2 file was updated in-place with encrypted internal payloads.", "The selected PK2 file was updated in-place with encrypted internal payloads."),
            ("The selected PK2 file was updated in-place with plain internal payloads.", "The selected PK2 file was updated in-place with plain internal payloads."),
            ("Extracting all PK2 files...", "Extracting all PK2 files..."),
            ("Extracting selected PK2 files...", "Extracting selected PK2 files..."),
            ("Raw encrypted payloads were preserved under the output folder.", "Raw encrypted payloads were preserved under the output folder."),
            ("Files were restored/decrypted under the output folder.", "Files were restored/decrypted under the output folder."),
            ("PK2 internal payload encryption completed.", "PK2 internal payload encryption completed."),
            ("PK2 internal payload decryption completed.", "PK2 internal payload decryption completed."),
            ("GFXFileManager.dll can decrypt these payloads while the client reads the PK2.", "GFXFileManager.dll can decrypt these payloads while the client reads the PK2."),
            ("The same PK2 file was restored to plain internal payloads.", "The same PK2 file was restored to plain internal payloads."),
            ("Preparing PK2 build...", "Preparing PK2 build..."),
            ("PK2 build completed.", "PK2 build completed."),
            ("Encrypting PK2 payloads", "Encrypting PK2 payloads"),
            ("Decrypting PK2 payloads", "Decrypting PK2 payloads"),
            ("Preparing encrypted import", "Preparing encrypted import"),
            ("Preparing plain import", "Preparing plain import"),
            ("Injecting folder into PK2", "Injecting folder into PK2"),
            ("Injecting folder into PK2...", "Injecting folder into PK2..."),
            ("Import completed with encrypted internal payloads.", "Import completed with encrypted internal payloads."),
            ("Import completed with plain internal payloads.", "Import completed with plain internal payloads."),
            ("Extracting raw PK2 payloads", "Extracting raw PK2 payloads"),
            ("Extracting plain PK2 files", "Extracting plain PK2 files"),
            ("Preparing PK2 directory layout...", "Preparing PK2 directory layout..."),
            ("Scanning folder: ", "Scanning folder: "),
            ("Scanning file: ", "Scanning file: "),
            ("Extracting ", "Extracting "));

        Add("ar",
            ("Parent folder", "المجلد السابق"),
            ("Folder view: {0} - {1:N0} folder(s), {2:N0} file(s).", "عرض المجلد: {0} - {1:N0} مجلد، {2:N0} ملف."),
            ("Prepared {0:N0} file(s), {1}.", "تم تجهيز {0:N0} ملف، {1}."),
            ("PK2 cache loaded instantly: {0} - {1:N0} internal file(s).", "تم تحميل كاش PK2 فورًا: {0} - {1:N0} ملف داخلي."),
            ("PK2 selected: {0} - {1:N0} internal file(s), {2}.", "تم اختيار PK2: {0} - {1:N0} ملف داخلي، {2}."),
            ("Extractor cache loaded instantly: {0} - {1:N0} internal file(s).", "تم تحميل كاش الاستخراج فورًا: {0} - {1:N0} ملف داخلي."),
            ("Extractor ready: {0} - {1:N0} internal file(s), {2}.", "الاستخراج جاهز: {0} - {1:N0} ملف داخلي، {2}."),
            ("No internal files were listed from this PK2.", "لم يتم عرض أي ملفات داخلية من ملف PK2 هذا."),
            ("Double-click folders to browse internal PK2 contents like Windows Explorer.", "انقر نقرًا مزدوجًا على المجلدات لتصفح محتويات PK2 مثل مستكشف ويندوز."),
            ("No internal files were listed. Make sure the selected archive is a real Silkroad PK2 and GFXFileManager.dll is rebuilt from this source.", "لم يتم عرض أي ملفات داخلية. تأكد أن الأرشيف المحدد PK2 حقيقي للعبة Silkroad وأن GFXFileManager.dll مبني من هذا السورس."),
            ("PK2 root folder.", "مجلد جذر PK2."),
            ("PK2 folder: {0}", "مجلد PK2: {0}"),
            ("This will {0} {1:N0} file(s) in the selected folder using the same names and paths. No backup files will be created. Continue?", "سيتم تنفيذ {0} على {1:N0} ملف داخل المجلد المحدد بنفس الأسماء والمسارات. لن يتم إنشاء نسخ احتياطية. هل تريد المتابعة؟"),
            ("This will {0} all internal file payloads inside the selected PK2 in-place. Encrypted payloads require the matching GFXFileManager.dll. The PK2 filename and path will not change. Continue?", "سيتم تنفيذ {0} لكل الـ payload الداخلي داخل ملف PK2 المحدد في مكانه. الـ payload المشفر يحتاج GFXFileManager.dll المطابق. لن يتغير اسم أو مسار ملف PK2. هل تريد المتابعة؟"),
            ("the client PK2 folder '{0}'", "مجلد PK2 الخاص بالعميل '{0}'"),
            ("This will inject the selected folder into {0}. Folder structure will be preserved and all PK2 payloads will be stored as {1} data. Continue?", "سيتم حقن المجلد المحدد داخل {0}. سيتم الحفاظ على بنية المجلدات وسيتم تخزين كل payload داخل PK2 كبيانات {1}. هل تريد المتابعة؟"),
            ("{0:N0} selected PK2 file(s)", "{0:N0} ملف PK2 محدد"),
            ("This will extract {0} to the output folder and keep the internal PK2 folder layout. Output files will be {1}. Continue?", "سيتم استخراج {0} إلى مجلد الإخراج مع الحفاظ على بنية مجلدات PK2 الداخلية. ستكون ملفات الإخراج {1}. هل تريد المتابعة؟"),
            ("This will build {0:N0} PK2 archive(s) with {1} and {2}. Encrypted payloads require the matching GFXFileManager.dll. Continue?", "سيتم بناء {0:N0} أرشيف PK2 باستخدام {1} و {2}. الـ payload المشفر يحتاج GFXFileManager.dll المطابق. هل تريد المتابعة؟"),
            ("Starting {0}...", "بدء {0}..."),
            ("{0}: {1:N0} / {2:N0}", "{0}: {1:N0} / {2:N0}"),
            ("Finished. {0:N0} file(s) processed, {1:N0} skipped.", "انتهت العملية. تمت معالجة {0:N0} ملف، وتم تخطي {1:N0}."),
            ("Finished with errors. {0:N0} processed, {1:N0} skipped, {2:N0} failed.", "انتهت العملية مع أخطاء. تمت معالجة {0:N0}، وتم تخطي {1:N0}، وفشل {2:N0}."),
            ("Cancelled. {0:N0} file(s) completed before cancellation.", "تم الإلغاء. اكتمل {0:N0} ملف قبل الإلغاء."),
            ("Building {0}.pk2 ({1:N0}/{2:N0})...", "جاري بناء {0}.pk2 ({1:N0}/{2:N0})..."),
            ("Building {0}.pk2 ({1:N0}/{2:N0})", "جاري بناء {0}.pk2 ({1:N0}/{2:N0})"),
            ("Building", "جاري البناء"),
            ("Finalizing", "جاري الإنهاء"),
            ("Selected PK2 archives were created with encrypted internal payloads readable through GFXFileManager.dll.", "تم إنشاء أرشيفات PK2 المحددة مع payload داخلي مشفر قابل للقراءة عبر GFXFileManager.dll."),
            ("Selected PK2 archives were created with plain internal payloads.", "تم إنشاء أرشيفات PK2 المحددة مع payload داخلي عادي."),
            ("The selected PK2 file was updated in-place with encrypted internal payloads.", "تم تحديث ملف PK2 المحدد في مكانه مع payload داخلي مشفر."),
            ("The selected PK2 file was updated in-place with plain internal payloads.", "تم تحديث ملف PK2 المحدد في مكانه مع payload داخلي عادي."),
            ("Extracting all PK2 files...", "جاري استخراج كل ملفات PK2..."),
            ("Extracting selected PK2 files...", "جاري استخراج ملفات PK2 المحددة..."),
            ("Raw encrypted payloads were preserved under the output folder.", "تم حفظ الـ payload المشفر الخام داخل مجلد الإخراج."),
            ("Files were restored/decrypted under the output folder.", "تم استعادة/فك تشفير الملفات داخل مجلد الإخراج."),
            ("PK2 internal payload encryption completed.", "اكتمل تشفير الـ payload الداخلي لملف PK2."),
            ("PK2 internal payload decryption completed.", "اكتمل فك تشفير الـ payload الداخلي لملف PK2."),
            ("GFXFileManager.dll can decrypt these payloads while the client reads the PK2.", "يمكن لـ GFXFileManager.dll فك تشفير هذا الـ payload أثناء قراءة العميل لملف PK2."),
            ("The same PK2 file was restored to plain internal payloads.", "تمت استعادة نفس ملف PK2 إلى payload داخلي عادي."),
            ("Preparing PK2 build...", "جاري تجهيز بناء PK2..."),
            ("PK2 build completed.", "اكتمل بناء PK2."),
            ("Encrypting PK2 payloads", "جاري تشفير Payload داخل PK2"),
            ("Decrypting PK2 payloads", "جاري فك تشفير Payload داخل PK2"),
            ("Preparing encrypted import", "جاري تجهيز استيراد مشفر"),
            ("Preparing plain import", "جاري تجهيز استيراد عادي"),
            ("Injecting folder into PK2", "جاري حقن المجلد داخل PK2"),
            ("Injecting folder into PK2...", "جاري حقن المجلد داخل PK2..."),
            ("Import completed with encrypted internal payloads.", "اكتمل الاستيراد مع Payload داخلي مشفر."),
            ("Import completed with plain internal payloads.", "اكتمل الاستيراد مع Payload داخلي عادي."),
            ("Extracting raw PK2 payloads", "جاري استخراج Payload خام من PK2"),
            ("Extracting plain PK2 files", "جاري استخراج ملفات PK2 عادية"),
            ("Preparing PK2 directory layout...", "جاري تجهيز بنية مجلدات PK2..."),
            ("Scanning folder: ", "جاري فحص المجلد: "),
            ("Scanning file: ", "جاري فحص الملف: "),
            ("Extracting ", "جاري استخراج "),
            ("GFX Compatible\r\nPayload Secure      ●\r\nv1.0.0", "متوافق مع GFX\r\nPayload مؤمّن      ●\r\nv1.0.0"),
            ("PK2 workflow\r\n• Build PK2 archives\r\n• Extract PK2 files\r\n• Import and secure payloads", "سير عمل PK2\r\n• بناء أرشيفات PK2\r\n• استخراج ملفات PK2\r\n• استيراد وحماية الـ Payload"),
            ("Protect folders or internal PK2 payloads without changing the readable PK2 header.", "احمِ المجلدات أو الـ Payload الداخلي داخل PK2 بدون تغيير هيدر PK2 المقروء."),
            ("Mode\r\nFolder or PK2", "الوضع\r\nمجلد أو PK2"),
            ("Output\r\nIn-place secure payload", "الإخراج\r\nحماية الـ Payload في مكانه"),
            ("Encryption target list", "قائمة أهداف التشفير"),
            ("Explorer preview opens the current folder instantly. Include subfolders affects the Start operation, not the preview navigation.", "معاينة المستكشف تفتح المجلد الحالي فورًا. خيار تضمين المجلدات الفرعية يؤثر على عملية البدء وليس على التنقل في المعاينة."),
            ("PK2 file mode encrypts or decrypts stored payloads inside the selected archive. Import and Extractor have their own workspaces.", "وضع ملف PK2 يشفر أو يفك تشفير الـ Payload المخزن داخل الأرشيف المحدد. الاستيراد والاستخراج لهما مساحات عمل مستقلة."),
            ("Choose a source, review the target list, then start the protected payload operation.", "اختر المصدر، راجع قائمة الأهداف، ثم ابدأ عملية حماية الـ Payload."),
            ("Each page has its own controls. Select Encryptor, Extractor, Import, or Builder from the sidebar.", "كل صفحة لها أدواتها الخاصة. اختر التشفير أو الاستخراج أو الاستيراد أو البناء من الشريط الجانبي."),
            ("Read the internal file tree, restore payloads, or export raw stored bytes.", "اقرأ شجرة الملفات الداخلية، أو استعد الـ Payload، أو صدّر البايتات الخام المخزنة."),
            ("Read\r\nPK2 contents", "قراءة\r\nمحتويات PK2"),
            ("Extract\r\nSelected or all", "استخراج\r\nالمحدد أو الكل"),
            ("Select the PK2 file to read...", "اختر ملف PK2 للقراءة..."),
            ("Select where the extracted files will be written...", "اختر مكان حفظ الملفات المستخرجة..."),
            ("Plain output restores custom payload encryption; raw output keeps bytes exactly as stored.", "الإخراج العادي يستعيد تشفير الـ Payload المخصص؛ الإخراج الخام يحتفظ بالبايتات كما هي مخزنة."),
            ("Select one or more internal files, or extract the complete archive.", "اختر ملفًا داخليًا واحدًا أو أكثر، أو استخرج الأرشيف كاملًا."),
            ("Import / Injection", "استيراد / حقن"),
            ("Inject update folders into the client archive and keep payload mode consistent for GFXFileManager.", "احقن مجلدات التحديث داخل أرشيف العميل وحافظ على وضع الـ Payload متوافقًا مع GFXFileManager."),
            ("Target\r\nExisting PK2", "الهدف\r\nPK2 موجود"),
            ("Mode\r\nPlain or encrypted", "الوضع\r\nعادي أو مشفر"),
            ("Select the client .pk2 file to inject into...", "اختر ملف .pk2 الخاص بالعميل للحقن داخله..."),
            ("Select the folder that contains the files to inject...", "اختر المجلد الذي يحتوي على الملفات المراد حقنها..."),
            ("Encrypted mode is readable only through the matching GFXFileManager.dll.", "الوضع المشفر لا يمكن قراءته إلا عبر GFXFileManager.dll المطابق."),
            ("Choose Target PK2, choose Import source folder, select payload state, then Import.", "اختر ملف PK2 الهدف، ثم مجلد مصدر الاستيراد، ثم حالة الـ Payload، وبعدها اضغط استيراد."),
            ("Import notes\r\n\r\nThe selected archive is updated in-place. Choose a folder such as Data, Media, Map, Music, Particles, or a folder containing those folders. Plain/encrypted payload mode is applied consistently to the whole PK2 so GFXFileManager.dll can read it correctly.", "ملاحظات الاستيراد\r\n\r\nيتم تحديث الأرشيف المحدد في مكانه. اختر مجلدًا مثل Data أو Media أو Map أو Music أو Particles، أو مجلدًا يحتوي على هذه المجلدات. يتم تطبيق وضع الـ Payload العادي/المشفر بشكل موحد على ملف PK2 بالكامل حتى يتمكن GFXFileManager.dll من قراءته بشكل صحيح."),
            ("Create Data.pk2, Media.pk2, Map.pk2, Music.pk2, or Particles.pk2 from source folders.", "أنشئ Data.pk2 أو Media.pk2 أو Map.pk2 أو Music.pk2 أو Particles.pk2 من مجلدات المصدر."),
            ("Queue\r\nKnown client folders", "قائمة\r\nمجلدات العميل المعروفة"),
            ("Build\r\nGFX compatible", "بناء\r\nمتوافق مع GFX"),
            ("Select client root, or one folder such as Media/Data/Map...", "اختر جذر العميل، أو مجلدًا واحدًا مثل Media/Data/Map..."),
            ("Select where Data.pk2 / Media.pk2 / Map.pk2 will be created...", "اختر مكان إنشاء Data.pk2 / Media.pk2 / Map.pk2..."),
            ("Encrypted payloads are decrypted by GFXFileManager.dll at runtime.", "يتم فك تشفير الـ Payload المشفر بواسطة GFXFileManager.dll أثناء التشغيل."),
            ("No files were found in the selected folder.", "لم يتم العثور على ملفات داخل المجلد المحدد."),
            ("The selected file is not .pk2. Continue anyway?", "الملف المحدد ليس .pk2. هل تريد المتابعة على أي حال؟"),
            ("Please select a valid target PK2 file.", "اختر ملف PK2 هدفًا صحيحًا."),
            ("The selected target file is not .pk2. Continue anyway?", "ملف الهدف المحدد ليس .pk2. هل تريد المتابعة على أي حال؟"),
            ("Please select a valid import source folder.", "اختر مجلد مصدر استيراد صحيحًا."),
            ("Please select a valid PK2 file to extract.", "اختر ملف PK2 صحيحًا للاستخراج."),
            ("Please select one or more PK2 files from the extractor list.", "اختر ملف PK2 واحدًا أو أكثر من قائمة الاستخراج."),
            ("Select at least one source folder to build.", "اختر مجلد مصدر واحدًا على الأقل للبناء."),
            ("Select folder to encrypt or decrypt in-place", "اختر مجلدًا لتشفيره أو فك تشفيره في مكانه"),
            ("Select PK2 file", "اختر ملف PK2"),
            ("Select PK2 file to extract", "اختر ملف PK2 لاستخراجه"),
            ("Select folder for extracted PK2 files", "اختر مجلدًا لملفات PK2 المستخرجة"),
            ("Select PK2 file to import into", "اختر ملف PK2 للاستيراد داخله"),
            ("Select file to import into PK2", "اختر ملفًا لاستيراده داخل PK2"),
            ("Select source folder to inject into the selected client PK2", "اختر مجلد المصدر لحقنه داخل ملف PK2 الخاص بالعميل المحدد"),
            ("Select client root or a single PK2 source folder", "اختر جذر العميل أو مجلد مصدر PK2 واحد"),
            ("Select output folder for built PK2 archives", "اختر مجلد الإخراج لأرشيفات PK2 المبنية"));

        // Keep every language selectable and every UI key resolved without falling
        // back to visible English for non-English languages.
        foreach(var profile in LanguageProfiles)
        {
            if(string.Equals(profile.Code, "en", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if(!data.TryGetValue(profile.Code, out var map))
            {
                map = new Dictionary<string, string>(StringComparer.Ordinal);
                data[profile.Code] = map;
            }
            foreach(var pair in data["en"])
            {
                if(!map.ContainsKey(pair.Key))
                {
                    map[pair.Key] = CompleteMissingTranslation(profile.Code, pair.Key, pair.Value);
                }
            }
        }

        return data;
    }

    private static string CompleteMissingTranslation(string code, string key, string english)
    {
        string L(string ar, string tr, string fr, string es, string de, string pl, string hu, string nl, string el, string pt, string hi, string ko, string zhHans, string zhHant, string th, string fa) => code switch
        {
            "ar" => ar,
            "tr" => tr,
            "fr" => fr,
            "es" => es,
            "de" => de,
            "pl" => pl,
            "hu" => hu,
            "nl" => nl,
            "el" => el,
            "pt-BR" => pt,
            "hi" => hi,
            "ko" => ko,
            "zh-Hans" => zhHans,
            "zh-Hant" => zhHant,
            "th" => th,
            "fa" => fa,
            _ => english
        };

        switch(key)
        {
            case "GFX Compatible\r\nPayload Secure      ●\r\nv1.0.0":
                return L("متوافق مع GFX\r\nPayload مؤمّن      ●\r\nv1.0.0", "GFX uyumlu\r\nPayload güvenli      ●\r\nv1.0.0", "Compatible GFX\r\nPayload sécurisé      ●\r\nv1.0.0", "Compatible con GFX\r\nPayload seguro      ●\r\nv1.0.0", "GFX-kompatibel\r\nPayload sicher      ●\r\nv1.0.0", "Zgodne z GFX\r\nPayload bezpieczny      ●\r\nv1.0.0", "GFX kompatibilis\r\nBiztonságos payload      ●\r\nv1.0.0", "GFX-compatibel\r\nPayload beveiligd      ●\r\nv1.0.0", "Συμβατό με GFX\r\nΑσφαλές payload      ●\r\nv1.0.0", "Compatível com GFX\r\nPayload seguro      ●\r\nv1.0.0", "GFX संगत\r\nPayload सुरक्षित      ●\r\nv1.0.0", "GFX 호환\r\nPayload 보안      ●\r\nv1.0.0", "GFX 兼容\r\nPayload 安全      ●\r\nv1.0.0", "GFX 相容\r\nPayload 安全      ●\r\nv1.0.0", "รองรับ GFX\r\nPayload ปลอดภัย      ●\r\nv1.0.0", "سازگار با GFX\r\nPayload امن      ●\r\nv1.0.0");
            case "PK2 workflow\r\n• Build PK2 archives\r\n• Extract PK2 files\r\n• Import and secure payloads":
                return L("سير عمل PK2\r\n• بناء أرشيفات PK2\r\n• استخراج ملفات PK2\r\n• استيراد وحماية الـ Payload", "PK2 iş akışı\r\n• PK2 arşivleri oluştur\r\n• PK2 dosyalarını çıkar\r\n• Payloadları içe aktar ve koru", "Flux PK2\r\n• Créer des archives PK2\r\n• Extraire les fichiers PK2\r\n• Importer et sécuriser les payloads", "Flujo PK2\r\n• Crear archivos PK2\r\n• Extraer archivos PK2\r\n• Importar y proteger payloads", "PK2-Ablauf\r\n• PK2-Archive erstellen\r\n• PK2-Dateien extrahieren\r\n• Payloads importieren und schützen", "Przepływ PK2\r\n• Buduj archiwa PK2\r\n• Wyodrębniaj pliki PK2\r\n• Importuj i zabezpieczaj payloady", "PK2 munkafolyamat\r\n• PK2 archívumok építése\r\n• PK2 fájlok kicsomagolása\r\n• Payloadok importálása és védelme", "PK2-werkstroom\r\n• PK2-archieven bouwen\r\n• PK2-bestanden uitpakken\r\n• Payloads importeren en beveiligen", "Ροή PK2\r\n• Δημιουργία αρχείων PK2\r\n• Εξαγωγή αρχείων PK2\r\n• Εισαγωγή και ασφάλιση payloads", "Fluxo PK2\r\n• Criar arquivos PK2\r\n• Extrair arquivos PK2\r\n• Importar e proteger payloads", "PK2 कार्यप्रवाह\r\n• PK2 आर्काइव बनाएं\r\n• PK2 फ़ाइलें निकालें\r\n• Payload आयात और सुरक्षित करें", "PK2 작업 흐름\r\n• PK2 아카이브 빌드\r\n• PK2 파일 추출\r\n• Payload 가져오기 및 보호", "PK2 工作流\r\n• 构建 PK2 封包\r\n• 提取 PK2 文件\r\n• 导入并保护 Payload", "PK2 工作流程\r\n• 建置 PK2 封包\r\n• 提取 PK2 檔案\r\n• 匯入並保護 Payload", "เวิร์กโฟลว์ PK2\r\n• สร้าง PK2 archive\r\n• แยกไฟล์ PK2\r\n• นำเข้าและป้องกัน Payload", "گردش کار PK2\r\n• ساخت آرشیوهای PK2\r\n• استخراج فایل‌های PK2\r\n• وارد کردن و ایمن‌سازی Payload");
            case "Mode\r\nFolder or PK2":
                return L("الوضع\r\nمجلد أو PK2", "Mod\r\nKlasör veya PK2", "Mode\r\nDossier ou PK2", "Modo\r\nCarpeta o PK2", "Modus\r\nOrdner oder PK2", "Tryb\r\nFolder lub PK2", "Mód\r\nMappa vagy PK2", "Modus\r\nMap of PK2", "Λειτουργία\r\nΦάκελος ή PK2", "Modo\r\nPasta ou PK2", "मोड\r\nफ़ोल्डर या PK2", "모드\r\n폴더 또는 PK2", "模式\r\n文件夹或 PK2", "模式\r\n資料夾或 PK2", "โหมด\r\nโฟลเดอร์หรือ PK2", "حالت\r\nپوشه یا PK2");
            case "Output\r\nIn-place secure payload":
                return L("الإخراج\r\nحماية الـ Payload في مكانه", "Çıktı\r\nYerinde güvenli payload", "Sortie\r\nPayload sécurisé sur place", "Salida\r\nPayload seguro in situ", "Ausgabe\r\nPayload direkt schützen", "Wyjście\r\nZabezpieczenie payloadu w miejscu", "Kimenet\r\nPayload védelem helyben", "Uitvoer\r\nPayload ter plekke beveiligen", "Έξοδος\r\nΑσφαλές payload επί τόπου", "Saída\r\nPayload seguro no local", "आउटपुट\r\nजगह पर सुरक्षित Payload", "출력\r\n제자리 Payload 보호", "输出\r\n就地保护 Payload", "輸出\r\n就地保護 Payload", "เอาต์พุต\r\nป้องกัน Payload ในที่เดิม", "خروجی\r\nایمن‌سازی Payload در محل");
            case "Read\r\nPK2 contents":
                return L("قراءة\r\nمحتويات PK2", "Oku\r\nPK2 içeriği", "Lire\r\nContenu PK2", "Leer\r\nContenido PK2", "Lesen\r\nPK2-Inhalt", "Czytaj\r\nZawartość PK2", "Olvasás\r\nPK2 tartalom", "Lezen\r\nPK2-inhoud", "Ανάγνωση\r\nΠεριεχομένων PK2", "Ler\r\nConteúdo PK2", "पढ़ें\r\nPK2 सामग्री", "읽기\r\nPK2 내용", "读取\r\nPK2 内容", "讀取\r\nPK2 內容", "อ่าน\r\nเนื้อหา PK2", "خواندن\r\nمحتوای PK2");
            case "Extract\r\nSelected or all":
                return L("استخراج\r\nالمحدد أو الكل", "Çıkar\r\nSeçili veya tümü", "Extraire\r\nSélection ou tout", "Extraer\r\nSeleccionados o todo", "Extrahieren\r\nAuswahl oder alles", "Wyodrębnij\r\nWybrane lub wszystko", "Kicsomagolás\r\nKijelölt vagy összes", "Uitpakken\r\nSelectie of alles", "Εξαγωγή\r\nΕπιλεγμένα ή όλα", "Extrair\r\nSelecionados ou tudo", "निकालें\r\nचयनित या सभी", "추출\r\n선택 또는 전체", "提取\r\n所选或全部", "提取\r\n所選或全部", "แยก\r\nที่เลือกหรือทั้งหมด", "استخراج\r\nانتخاب‌شده یا همه");
            case "Target\r\nExisting PK2":
                return L("الهدف\r\nPK2 موجود", "Hedef\r\nMevcut PK2", "Cible\r\nPK2 existant", "Destino\r\nPK2 existente", "Ziel\r\nVorhandene PK2", "Cel\r\nIstniejący PK2", "Cél\r\nMeglévő PK2", "Doel\r\nBestaande PK2", "Στόχος\r\nΥπάρχον PK2", "Destino\r\nPK2 existente", "लक्ष्य\r\nमौजूदा PK2", "대상\r\n기존 PK2", "目标\r\n现有 PK2", "目標\r\n現有 PK2", "เป้าหมาย\r\nPK2 ที่มีอยู่", "هدف\r\nPK2 موجود");
            case "Mode\r\nPlain or encrypted":
                return L("الوضع\r\nعادي أو مشفر", "Mod\r\nDüz veya şifreli", "Mode\r\nClair ou chiffré", "Modo\r\nPlano o cifrado", "Modus\r\nKlar oder verschlüsselt", "Tryb\r\nZwykły lub szyfrowany", "Mód\r\nSima vagy titkosított", "Modus\r\nNormaal of versleuteld", "Λειτουργία\r\nΑπλό ή κρυπτογραφημένο", "Modo\r\nSimples ou criptografado", "मोड\r\nसादा या एन्क्रिप्टेड", "모드\r\n일반 또는 암호화", "模式\r\n普通或加密", "模式\r\n普通或加密", "โหมด\r\nปกติหรือเข้ารหัส", "حالت\r\nساده یا رمزگذاری‌شده");
            case "Queue\r\nKnown client folders":
                return L("قائمة\r\nمجلدات العميل المعروفة", "Kuyruk\r\nBilinen istemci klasörleri", "File\r\nDossiers client connus", "Cola\r\nCarpetas de cliente conocidas", "Warteschlange\r\nBekannte Clientordner", "Kolejka\r\nZnane foldery klienta", "Sor\r\nIsmert kliens mappák", "Wachtrij\r\nBekende clientmappen", "Ουρά\r\nΓνωστοί φάκελοι client", "Fila\r\nPastas conhecidas do cliente", "क्यू\r\nज्ञात क्लाइंट फ़ोल्डर", "대기열\r\n알려진 클라이언트 폴더", "队列\r\n已知客户端文件夹", "佇列\r\n已知客戶端資料夾", "คิว\r\nโฟลเดอร์ไคลเอนต์ที่รู้จัก", "صف\r\nپوشه‌های شناخته‌شده کلاینت");
            case "Build\r\nGFX compatible":
                return L("بناء\r\nمتوافق مع GFX", "Oluştur\r\nGFX uyumlu", "Créer\r\nCompatible GFX", "Crear\r\nCompatible con GFX", "Erstellen\r\nGFX-kompatibel", "Buduj\r\nZgodne z GFX", "Építés\r\nGFX kompatibilis", "Bouwen\r\nGFX-compatibel", "Δημιουργία\r\nΣυμβατό με GFX", "Criar\r\nCompatível com GFX", "बनाएं\r\nGFX संगत", "빌드\r\nGFX 호환", "构建\r\nGFX 兼容", "建置\r\nGFX 相容", "สร้าง\r\nรองรับ GFX", "ساخت\r\nسازگار با GFX");
        }

        if(key.StartsWith("Folder view:", StringComparison.Ordinal)) return L("عرض المجلد: {0} - {1:N0} مجلد، {2:N0} ملف.", "Klasör görünümü: {0} - {1:N0} klasör, {2:N0} dosya.", "Vue dossier : {0} - {1:N0} dossier(s), {2:N0} fichier(s).", "Vista de carpeta: {0} - {1:N0} carpeta(s), {2:N0} archivo(s).", "Ordneransicht: {0} - {1:N0} Ordner, {2:N0} Datei(en).", "Widok folderu: {0} - {1:N0} folderów, {2:N0} plików.", "Mappanézet: {0} - {1:N0} mappa, {2:N0} fájl.", "Mapweergave: {0} - {1:N0} map(pen), {2:N0} bestand(en).", "Προβολή φακέλου: {0} - {1:N0} φάκελοι, {2:N0} αρχεία.", "Visualização da pasta: {0} - {1:N0} pasta(s), {2:N0} arquivo(s).", "फ़ोल्डर दृश्य: {0} - {1:N0} फ़ोल्डर, {2:N0} फ़ाइलें.", "폴더 보기: {0} - 폴더 {1:N0}개, 파일 {2:N0}개.", "文件夹视图：{0} - {1:N0} 个文件夹，{2:N0} 个文件。", "資料夾檢視：{0} - {1:N0} 個資料夾，{2:N0} 個檔案。", "มุมมองโฟลเดอร์: {0} - {1:N0} โฟลเดอร์, {2:N0} ไฟล์.", "نمای پوشه: {0} - {1:N0} پوشه، {2:N0} فایل.");
        if(key.StartsWith("Prepared {0", StringComparison.Ordinal)) return L("تم تجهيز {0:N0} ملف، {1}.", "{0:N0} dosya hazırlandı, {1}.", "{0:N0} fichier(s) préparé(s), {1}.", "{0:N0} archivo(s) preparados, {1}.", "{0:N0} Datei(en) vorbereitet, {1}.", "Przygotowano {0:N0} plików, {1}.", "{0:N0} fájl előkészítve, {1}.", "{0:N0} bestand(en) voorbereid, {1}.", "Προετοιμάστηκαν {0:N0} αρχεία, {1}.", "{0:N0} arquivo(s) preparados, {1}.", "{0:N0} फ़ाइलें तैयार, {1}.", "{0:N0}개 파일 준비됨, {1}.", "已准备 {0:N0} 个文件，{1}。", "已準備 {0:N0} 個檔案，{1}。", "เตรียมไฟล์ {0:N0} ไฟล์แล้ว, {1}.", "{0:N0} فایل آماده شد، {1}.");
        if(key.Contains("cache loaded instantly", StringComparison.Ordinal)) return L("تم تحميل الكاش فورًا: {0} - {1:N0} ملف داخلي.", "Önbellek anında yüklendi: {0} - {1:N0} iç dosya.", "Cache chargé instantanément : {0} - {1:N0} fichier(s) interne(s).", "Caché cargada al instante: {0} - {1:N0} archivo(s) interno(s).", "Cache sofort geladen: {0} - {1:N0} interne Datei(en).", "Pamięć podręczna załadowana od razu: {0} - {1:N0} plików wewnętrznych.", "Gyorsítótár azonnal betöltve: {0} - {1:N0} belső fájl.", "Cache direct geladen: {0} - {1:N0} interne bestand(en).", "Η cache φορτώθηκε άμεσα: {0} - {1:N0} εσωτερικά αρχεία.", "Cache carregado instantaneamente: {0} - {1:N0} arquivo(s) interno(s).", "कैश तुरंत लोड हुआ: {0} - {1:N0} आंतरिक फ़ाइलें.", "캐시 즉시 로드됨: {0} - 내부 파일 {1:N0}개.", "缓存已即时加载：{0} - {1:N0} 个内部文件。", "快取已立即載入：{0} - {1:N0} 個內部檔案。", "โหลดแคชทันที: {0} - {1:N0} ไฟล์ภายใน.", "کش فوراً بارگیری شد: {0} - {1:N0} فایل داخلی.");
        if(key.StartsWith("PK2 selected:", StringComparison.Ordinal) || key.StartsWith("Extractor ready:", StringComparison.Ordinal)) return L("جاهز: {0} - {1:N0} ملف داخلي، {2}.", "Hazır: {0} - {1:N0} iç dosya, {2}.", "Prêt : {0} - {1:N0} fichier(s) interne(s), {2}.", "Listo: {0} - {1:N0} archivo(s) interno(s), {2}.", "Bereit: {0} - {1:N0} interne Datei(en), {2}.", "Gotowe: {0} - {1:N0} plików wewnętrznych, {2}.", "Kész: {0} - {1:N0} belső fájl, {2}.", "Gereed: {0} - {1:N0} interne bestand(en), {2}.", "Έτοιμο: {0} - {1:N0} εσωτερικά αρχεία, {2}.", "Pronto: {0} - {1:N0} arquivo(s) interno(s), {2}.", "तैयार: {0} - {1:N0} आंतरिक फ़ाइलें, {2}.", "준비됨: {0} - 내부 파일 {1:N0}개, {2}.", "就绪：{0} - {1:N0} 个内部文件，{2}。", "就緒：{0} - {1:N0} 個內部檔案，{2}。", "พร้อม: {0} - {1:N0} ไฟล์ภายใน, {2}.", "آماده: {0} - {1:N0} فایل داخلی، {2}.");
        if(key == "{0}: {1:N0} / {2:N0}") return "{0}: {1:N0} / {2:N0}";
        if(key.StartsWith("Building {0}.pk2", StringComparison.Ordinal)) return L("جاري بناء {0}.pk2 ({1:N0}/{2:N0})", "{0}.pk2 oluşturuluyor ({1:N0}/{2:N0})", "Création de {0}.pk2 ({1:N0}/{2:N0})", "Creando {0}.pk2 ({1:N0}/{2:N0})", "{0}.pk2 wird erstellt ({1:N0}/{2:N0})", "Budowanie {0}.pk2 ({1:N0}/{2:N0})", "{0}.pk2 építése ({1:N0}/{2:N0})", "{0}.pk2 bouwen ({1:N0}/{2:N0})", "Δημιουργία {0}.pk2 ({1:N0}/{2:N0})", "Criando {0}.pk2 ({1:N0}/{2:N0})", "{0}.pk2 बन रहा है ({1:N0}/{2:N0})", "{0}.pk2 빌드 중 ({1:N0}/{2:N0})", "正在构建 {0}.pk2（{1:N0}/{2:N0}）", "正在建置 {0}.pk2（{1:N0}/{2:N0}）", "กำลังสร้าง {0}.pk2 ({1:N0}/{2:N0})", "در حال ساخت {0}.pk2 ({1:N0}/{2:N0})");
        if(key.Contains("Continue?", StringComparison.Ordinal) || key.EndsWith("anyway?", StringComparison.Ordinal)) return L("هل تريد المتابعة؟", "Devam edilsin mi?", "Continuer ?", "¿Continuar?", "Fortfahren?", "Kontynuować?", "Folytatja?", "Doorgaan?", "Συνέχεια;", "Continuar?", "क्या जारी रखें?", "계속할까요?", "是否继续？", "是否繼續？", "ต้องการดำเนินการต่อหรือไม่?", "ادامه داده شود؟");
        if(key.StartsWith("Select ", StringComparison.Ordinal) || key.StartsWith("Please select", StringComparison.Ordinal)) return L("اختر قيمة صحيحة.", "Geçerli bir seçim yapın.", "Sélectionnez une valeur valide.", "Seleccione un valor válido.", "Wählen Sie einen gültigen Wert.", "Wybierz prawidłową wartość.", "Válasszon érvényes értéket.", "Selecteer een geldige waarde.", "Επιλέξτε έγκυρη τιμή.", "Selecione um valor válido.", "कृपया मान्य विकल्प चुनें.", "올바른 값을 선택하세요.", "请选择有效值。", "請選擇有效值。", "โปรดเลือกค่าที่ถูกต้อง", "یک مقدار معتبر انتخاب کنید.");
        if(key.Contains("failed", StringComparison.OrdinalIgnoreCase) || key.Contains("Could not", StringComparison.Ordinal)) return L("تعذر إكمال العملية.", "İşlem tamamlanamadı.", "L’opération n’a pas pu être terminée.", "No se pudo completar la operación.", "Der Vorgang konnte nicht abgeschlossen werden.", "Nie można ukończyć operacji.", "A művelet nem fejezhető be.", "De bewerking kon niet worden voltooid.", "Δεν ήταν δυνατή η ολοκλήρωση της λειτουργίας.", "Não foi possível concluir a operação.", "कार्रवाई पूरी नहीं हो सकी.", "작업을 완료할 수 없습니다.", "无法完成操作。", "無法完成操作。", "ไม่สามารถดำเนินการให้เสร็จได้", "عملیات کامل نشد.");
        if(key.Contains("completed", StringComparison.OrdinalIgnoreCase) || key.Contains("Done", StringComparison.Ordinal)) return L("اكتملت العملية.", "İşlem tamamlandı.", "Opération terminée.", "Operación completada.", "Vorgang abgeschlossen.", "Operacja zakończona.", "A művelet befejeződött.", "Bewerking voltooid.", "Η λειτουργία ολοκληρώθηκε.", "Operação concluída.", "कार्रवाई पूरी हुई.", "작업이 완료되었습니다.", "操作已完成。", "操作已完成。", "ดำเนินการเสร็จสิ้น", "عملیات کامل شد.");
        if(key.Contains("folder", StringComparison.OrdinalIgnoreCase)) return L("معلومات المجلد.", "Klasör bilgisi.", "Informations du dossier.", "Información de carpeta.", "Ordnerinformation.", "Informacje o folderze.", "Mappainformáció.", "Mapinformatie.", "Πληροφορίες φακέλου.", "Informações da pasta.", "फ़ोल्डर जानकारी.", "폴더 정보.", "文件夹信息。", "資料夾資訊。", "ข้อมูลโฟลเดอร์", "اطلاعات پوشه.");
        if(key.Contains("PK2", StringComparison.OrdinalIgnoreCase)) return L("معلومات PK2.", "PK2 bilgisi.", "Informations PK2.", "Información de PK2.", "PK2-Information.", "Informacje PK2.", "PK2 információ.", "PK2-informatie.", "Πληροφορίες PK2.", "Informações de PK2.", "PK2 जानकारी.", "PK2 정보.", "PK2 信息。", "PK2 資訊。", "ข้อมูล PK2", "اطلاعات PK2.");

        return L("نص واجهة.", "Arayüz metni.", "Texte d’interface.", "Texto de interfaz.", "Oberflächentext.", "Tekst interfejsu.", "Felületi szöveg.", "Interfacetekst.", "Κείμενο διεπαφής.", "Texto da interface.", "इंटरफ़ेस पाठ.", "인터페이스 텍스트.", "界面文本。", "介面文字。", "ข้อความส่วนติดต่อ", "متن رابط کاربری.");
    }

    private string T(string key)
    {
        if(string.IsNullOrEmpty(key))
        {
            return key;
        }
        if(Translations.TryGetValue(_language.Code, out var languageMap) && languageMap.TryGetValue(key, out var value))
        {
            return value;
        }
        if(Translations.TryGetValue("en", out var englishMap) && englishMap.TryGetValue(key, out var english))
        {
            return english;
        }
        return key;
    }

    private string TF(string key, params object[] args) => string.Format(CultureInfo.CurrentCulture, T(key), args);

    private string TranslateExternalStatus(string text)
    {
        if(string.IsNullOrWhiteSpace(text))
        {
            return text;
        }
        foreach(var prefix in new[] { "Scanning folder: ", "Scanning file: ", "Extracting " })
        {
            if(text.StartsWith(prefix, StringComparison.Ordinal))
            {
                return T(prefix) + text[prefix.Length..];
            }
        }
        return TryResolveTranslationKey(text, out var key) ? T(key) : text;
    }

    private Control RegisterLocalizedText(Control control, string key)
    {
        if(control is null)
        {
            return control!;
        }
        _localizedControls[control] = key;
        control.Text = T(key);
        return control;
    }

    private void RegisterLocalizedPlaceholder(TextBox textBox, string key)
    {
        _localizedPlaceholders[textBox] = key;
        textBox.PlaceholderText = T(key);
    }

    private void LoadAppSettings()
    {
        _settings = new AppSettings();
        try
        {
            if(File.Exists(SettingsPath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
                if(loaded is not null)
                {
                    _settings = loaded;
                }
            }
        }
        catch
        {
            _settings = new AppSettings();
        }

        _language = LanguageProfiles.FirstOrDefault(x => string.Equals(x.Code, _settings.LanguageCode, StringComparison.OrdinalIgnoreCase)) ?? LanguageProfiles[0];
        _theme = ThemeProfiles.FirstOrDefault(x => string.Equals(x.Name, _settings.ThemeName, StringComparison.OrdinalIgnoreCase)) ?? ThemeProfiles[0];
    }

    private void SaveAppSettings()
    {
        try
        {
            _settings.ThemeName = _theme.Name;
            _settings.LanguageCode = _language.Code;
            _settings.BlowfishKey = string.IsNullOrWhiteSpace(_blowfishKeyBox.Text.Trim()) ? "169841" : _blowfishKeyBox.Text.Trim();
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(_settings, options));
        }
        catch
        {
        }
    }

    private void PopulateThemeCombo()
    {
        var selectedIndex = Array.IndexOf(ThemeProfiles, _theme);
        if(selectedIndex < 0)
        {
            selectedIndex = 0;
        }
        _themeBox.Items.Clear();
        foreach(var theme in ThemeProfiles)
        {
            _themeBox.Items.Add(T(theme.Name));
        }
        _themeBox.SelectedIndex = Math.Min(selectedIndex, _themeBox.Items.Count - 1);
    }

    private void PopulateLanguageCombo()
    {
        var selectedIndex = Array.FindIndex(LanguageProfiles, x => string.Equals(x.Code, _language.Code, StringComparison.OrdinalIgnoreCase));
        if(selectedIndex < 0)
        {
            selectedIndex = 0;
        }
        _languageBox.Items.Clear();
        foreach(var language in LanguageProfiles)
        {
            _languageBox.Items.Add(language.DisplayName);
        }
        _languageBox.SelectedIndex = Math.Min(selectedIndex, _languageBox.Items.Count - 1);
    }

    private void RefreshLocalizedComboItems()
    {
        // Do not toggle _applyingLanguage here. This method is called from ApplyLanguage(),
        // and resetting the guard while controls are still being repopulated can fire nested
        // SelectedIndexChanged events. That was the reason RTL languages could make the
        // window look like it is opening/closing quickly and then crash.
        PopulateThemeCombo();
        PopulateLanguageCombo();

        RefreshComboItems(_operationBox, new[] { "Encrypt", "Decrypt" });
        RefreshComboItems(_extractPayloadModeBox, new[] { "Extract restored/plain files", "Extract raw stored payload files" });
        RefreshComboItems(_importPayloadModeBox, new[] { "Store internal payloads plain", "Store internal payloads encrypted" });
        if(!_localizedControls.ContainsKey(_blowfishKeyBox))
        {
            _blowfishKeyBox.Text = string.IsNullOrWhiteSpace(_settings.BlowfishKey) ? "169841" : _settings.BlowfishKey;
        }
    }

    private void RefreshComboItems(ComboBox combo, string[] keys)
    {
        if(combo.Items.Count == 0)
        {
            return;
        }
        var selected = combo.SelectedIndex;
        combo.Items.Clear();
        foreach(var key in keys)
        {
            combo.Items.Add(T(key));
        }
        combo.SelectedIndex = Math.Clamp(selected, 0, combo.Items.Count - 1);
    }

    private void ApplyLanguage()
    {
        if(_applyingLanguage)
        {
            return;
        }

        _applyingLanguage = true;
        try
        {
            Text = T("PK2 Tools");
            var rtl = IsRtlLanguage(_language.Code);

            // Keep the form layout physically stable while switching languages.
            // Changing Form.RightToLeftLayout after the handle is created forces WinForms
            // to recreate the whole window and can create a loop with custom themed controls.
            // Text direction is applied per-control instead, so Arabic/Persian text reads
            // correctly without tearing down the main window.
            RightToLeft = RightToLeft.No;
            RightToLeftLayout = false;

            SuspendLayout();
            RefreshLocalizedComboItems();

            foreach(var pair in _localizedControls.ToArray())
            {
                var control = pair.Key;
                var key = pair.Value;
                if(!control.IsDisposed)
                {
                    control.Text = T(key);
                }
            }

            foreach(var pair in _localizedPlaceholders.ToArray())
            {
                var textBox = pair.Key;
                var key = pair.Value;
                if(!textBox.IsDisposed)
                {
                    textBox.PlaceholderText = T(key);
                }
            }

            var visited = new HashSet<Control>();
            void ApplyRecursive(Control control)
            {
                if(control is null || visited.Contains(control))
                {
                    return;
                }
                visited.Add(control);

                if(control is not TextBox and not ComboBox && !_localizedControls.ContainsKey(control) && TryResolveTranslationKey(control.Text, out var key))
                {
                    control.Text = T(key);
                    _localizedControls[control] = key;
                }

                if(control is TextBox textBox && !_localizedPlaceholders.ContainsKey(textBox) && TryResolveTranslationKey(textBox.PlaceholderText, out var placeholderKey))
                {
                    textBox.PlaceholderText = T(placeholderKey);
                    _localizedPlaceholders[textBox] = placeholderKey;
                }

                if(control is ListView list)
                {
                    foreach(ColumnHeader column in list.Columns)
                    {
                        if(TryResolveTranslationKey(column.Text, out var columnKey))
                        {
                            column.Text = T(columnKey);
                        }
                    }

                    foreach(ListViewItem item in list.Items)
                    {
                        for(var i = 0; i < item.SubItems.Count; i++)
                        {
                            if(TryResolveTranslationKey(item.SubItems[i].Text, out var itemKey))
                            {
                                item.SubItems[i].Text = T(itemKey);
                            }
                        }
                    }
                }

                foreach(Control child in control.Controls)
                {
                    ApplyRecursive(child);
                }
            }

            ApplyRecursive(this);
            ApplyRecursive(_encryptorPage);
            ApplyRecursive(_extractorPage);
            ApplyRecursive(_importPage);
            ApplyRecursive(_builderPage);
            ApplyRecursive(_folderTab);
            ApplyRecursive(_pk2Tab);
            RelocalizeListSurfaces();
            RefreshActionState();
            ApplyReadingDirection(rtl);
            Invalidate(true);
        }
        finally
        {
            ResumeLayout(true);
            _applyingLanguage = false;
        }

        _ = RefreshCurrentPagePreviewAsync();
    }

    private void RelocalizeListSurfaces()
    {
        ConfigureExplorerColumns(_previewList);
        ConfigureExplorerColumns(_extractPreviewList);
        ConfigureBuilderColumns();

        void RelocalizeItemStatuses(ListView list)
        {
            foreach(ListViewItem item in list.Items)
            {
                for(var i = 0; i < item.SubItems.Count; i++)
                {
                    if(TryResolveTranslationKey(item.SubItems[i].Text, out var itemKey))
                    {
                        item.SubItems[i].Text = T(itemKey);
                    }
                }
            }
            list.Invalidate();
        }

        RelocalizeItemStatuses(_previewList);
        RelocalizeItemStatuses(_extractPreviewList);
        RelocalizeItemStatuses(_builderQueueList);
    }

    private static bool IsRtlLanguage(string code)
    {
        return string.Equals(code, "ar", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "fa", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyReadingDirection(bool rtl)
    {
        var direction = rtl ? RightToLeft.Yes : RightToLeft.No;
        var left = rtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
        var bottomLeft = rtl ? ContentAlignment.BottomRight : ContentAlignment.BottomLeft;

        void Apply(Control control)
        {
            if(control is null || control.IsDisposed)
            {
                return;
            }

            // Do not mirror container coordinates. Only switch readable text controls.
            if(control is TextBox or ComboBox or ListBox or ListView or TreeView or Button or CheckBox or RadioButton)
            {
                control.RightToLeft = direction;
            }
            else
            {
                control.RightToLeft = RightToLeft.No;
            }

            if(control is Label label)
            {
                if(label.TextAlign == ContentAlignment.BottomLeft || label.TextAlign == ContentAlignment.BottomRight)
                {
                    label.TextAlign = bottomLeft;
                }
                else if(label.TextAlign == ContentAlignment.MiddleLeft || label.TextAlign == ContentAlignment.MiddleRight)
                {
                    label.TextAlign = left;
                }
            }

            foreach(Control child in control.Controls)
            {
                Apply(child);
            }
        }

        Apply(this);
    }

    private static bool TryResolveTranslationKey(string? text, out string key)
    {
        key = string.Empty;
        if(string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach(var languageMap in Translations.Values)
        {
            foreach(var pair in languageMap)
            {
                if(string.Equals(pair.Key, text, StringComparison.Ordinal) || string.Equals(pair.Value, text, StringComparison.Ordinal))
                {
                    key = pair.Key;
                    return true;
                }
            }
        }
        return false;
    }
}
