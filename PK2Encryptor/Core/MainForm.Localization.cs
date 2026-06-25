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
        public string SelectedProjectCode { get; set; } = "silkroad-original";
        public string Pk2BlowfishKey { get; set; } = DefaultPk2BlowfishKey;
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
            ("PROJECT", "PROJECT"),
            ("PK2 BLOWFISH KEY", "PK2 BLOWFISH KEY"),
            ("Optional PK2 key / password", "Optional PK2 key / password"),
            ("Default PK2 Blowfish key: 169841", "Default PK2 Blowfish key: 169841"),
            ("Silkroad-Orginal", "Silkroad-Orginal"),
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
            ("PK2 extraction completed.", "PK2 extraction completed."));

        Add("ar",
            ("PK2 Tools", "أدوات PK2"), ("Studio", "استوديو"), ("PK2 Tools - Studio", "PK2 Tools - Studio"), ("Build • Extract • Inject • Secure", "بناء • استخراج • حقن • حماية"), ("WORKSPACE", "مساحة العمل"), ("Encryptor", "التشفير"), ("Extractor", "الاستخراج"), ("Import / Inject", "استيراد / حقن"), ("Build PK2", "بناء PK2"), ("Folder mode", "وضع المجلد"), ("PK2 file mode", "وضع ملف PK2"), ("INTERFACE THEME", "ثيم الواجهة"), ("LANGUAGE", "اللغة"), ("Light Premium / Ivory Blue", "فاتح احترافي / أزرق عاجي"), ("Dark Premium / Obsidian Gold", "داكن احترافي / ذهبي"), ("GFX Compatible", "متوافق مع GFX"), ("Payload Secure", "Payload مؤمّن"), ("Folder path", "مسار المجلد"), ("Paste or browse the folder path...", "الصق أو اختر مسار المجلد..."), ("Browse", "استعراض"), ("Include subfolders", "تضمين المجلدات الفرعية"), ("Include hidden/system files", "تضمين الملفات المخفية/النظام"), ("PK2 file path", "مسار ملف PK2"), ("Paste or browse a .pk2 file path...", "الصق أو اختر مسار ملف .pk2..."), ("Name", "الاسم"), ("Size", "الحجم"), ("Type", "النوع"), ("State", "الحالة"), ("Operation", "العملية"), ("Encrypt", "تشفير"), ("Decrypt", "فك التشفير"), ("Start", "بدء"), ("Cancel", "إلغاء"), ("LIVE STATUS", "الحالة الحالية"), ("Choose a valid folder.", "اختر مجلدًا صحيحًا."), ("Status", "الحالة"), ("Ready.", "جاهز."), ("Ready", "جاهز"), ("Missing folder", "المجلد مفقود"), ("Queued", "في الانتظار"), ("Processing", "جاري المعالجة"), ("Done", "تم"), ("Failed", "فشل"), ("Cancelled", "تم الإلغاء"), ("Already encrypted", "مشفر بالفعل"), ("Not encrypted", "غير مشفر"), ("File", "ملف"), ("Folder", "مجلد"), ("Root", "الجذر"), ("Open", "فتح"), ("PK2 file", "ملف PK2"), ("Browse PK2", "استعراض PK2"), ("Output folder", "مجلد الإخراج"), ("Browse Output", "استعراض الإخراج"), ("Output file state", "حالة ملفات الإخراج"), ("Extract restored/plain files", "استخراج ملفات مستعادة/عادية"), ("Extract raw stored payload files", "استخراج payload الخام المخزن"), ("PK2 internal files", "ملفات PK2 الداخلية"), ("Extract Selected", "استخراج المحدد"), ("Extract All", "استخراج الكل"), ("Clear", "مسح"), ("Target PK2 file", "ملف PK2 الهدف"), ("Import source folder", "مجلد مصدر الاستيراد"), ("Browse Folder", "استعراض مجلد"), ("Stored payload state", "حالة payload المخزن"), ("Store internal payloads plain", "تخزين payload الداخلي عادي"), ("Store internal payloads encrypted", "تخزين payload الداخلي مشفر"), ("Import", "استيراد"), ("Source client/folder", "العميل/المجلد المصدر"), ("Browse Source", "استعراض المصدر"), ("Builder PK2 queue", "قائمة بناء PK2"), ("PK2 archive", "أرشيف PK2"), ("Source folder", "مجلد المصدر"), ("Output file", "ملف الإخراج"), ("Encrypt directory entries", "تشفير إدخالات الدليل"), ("Encrypt internal payloads", "تشفير payload الداخلي"), ("Refresh", "تحديث"), ("Build selected", "بناء المحدد"), ("Please select a valid folder.", "اختر مجلدًا صحيحًا."), ("Please select a valid PK2 file.", "اختر ملف PK2 صحيحًا."), ("Please select an output folder.", "اختر مجلد إخراج."), ("Operation completed successfully.", "اكتملت العملية بنجاح."), ("Builder PK2 completed successfully.", "تم بناء PK2 بنجاح."), ("Folder import completed.", "تم استيراد المجلد."), ("PK2 extraction completed.", "تم استخراج PK2."));

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


        Add("ar", ("PROJECT", "المشروع"), ("PK2 BLOWFISH KEY", "مفتاح Blowfish PK2"), ("Optional PK2 key / password", "مفتاح/كلمة مرور PK2 اختيارية"), ("Default PK2 Blowfish key: 169841", "المفتاح الافتراضي لـ PK2 Blowfish: 169841"));
        Add("tr", ("PROJECT", "PROJE"), ("PK2 BLOWFISH KEY", "PK2 BLOWFISH ANAHTARI"), ("Optional PK2 key / password", "İsteğe bağlı PK2 anahtarı / parola"));
        Add("fr", ("PROJECT", "PROJET"), ("PK2 BLOWFISH KEY", "CLÉ BLOWFISH PK2"), ("Optional PK2 key / password", "Clé / mot de passe PK2 optionnel"));
        Add("es", ("PROJECT", "PROYECTO"), ("PK2 BLOWFISH KEY", "CLAVE BLOWFISH PK2"), ("Optional PK2 key / password", "Clave / contraseña PK2 opcional"));
        Add("de", ("PROJECT", "PROJEKT"), ("PK2 BLOWFISH KEY", "PK2-BLOWFISH-SCHLÜSSEL"), ("Optional PK2 key / password", "Optionaler PK2-Schlüssel / Passwort"));
        Add("pl", ("PROJECT", "PROJEKT"), ("PK2 BLOWFISH KEY", "KLUCZ BLOWFISH PK2"), ("Optional PK2 key / password", "Opcjonalny klucz / hasło PK2"));
        Add("hu", ("PROJECT", "PROJEKT"), ("PK2 BLOWFISH KEY", "PK2 BLOWFISH KULCS"), ("Optional PK2 key / password", "Opcionális PK2 kulcs / jelszó"));
        Add("nl", ("PROJECT", "PROJECT"), ("PK2 BLOWFISH KEY", "PK2 BLOWFISH-SLEUTEL"), ("Optional PK2 key / password", "Optionele PK2-sleutel / wachtwoord"));
        Add("el", ("PROJECT", "ΕΡΓΟ"), ("PK2 BLOWFISH KEY", "ΚΛΕΙΔΙ BLOWFISH PK2"), ("Optional PK2 key / password", "Προαιρετικό κλειδί / κωδικός PK2"));
        Add("pt-BR", ("PROJECT", "PROJETO"), ("PK2 BLOWFISH KEY", "CHAVE BLOWFISH PK2"), ("Optional PK2 key / password", "Chave / senha PK2 opcional"));
        Add("hi", ("PROJECT", "प्रोजेक्ट"), ("PK2 BLOWFISH KEY", "PK2 Blowfish कुंजी"), ("Optional PK2 key / password", "वैकल्पिक PK2 कुंजी / पासवर्ड"));
        Add("ko", ("PROJECT", "프로젝트"), ("PK2 BLOWFISH KEY", "PK2 Blowfish 키"), ("Optional PK2 key / password", "선택 PK2 키 / 비밀번호"));
        Add("zh-Hans", ("PROJECT", "项目"), ("PK2 BLOWFISH KEY", "PK2 Blowfish 密钥"), ("Optional PK2 key / password", "可选 PK2 密钥 / 密码"));
        Add("zh-Hant", ("PROJECT", "專案"), ("PK2 BLOWFISH KEY", "PK2 Blowfish 金鑰"), ("Optional PK2 key / password", "選用 PK2 金鑰 / 密碼"));
        Add("th", ("PROJECT", "โปรเจกต์"), ("PK2 BLOWFISH KEY", "คีย์ Blowfish PK2"), ("Optional PK2 key / password", "คีย์ / รหัสผ่าน PK2 ไม่บังคับ"));
        Add("fa", ("PROJECT", "پروژه"), ("PK2 BLOWFISH KEY", "کلید Blowfish PK2"), ("Optional PK2 key / password", "کلید / رمز عبور اختیاری PK2"));

        return data;
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
            _settings.SelectedProjectCode = _activeProject.Code;
            _settings.Pk2BlowfishKey = GetCurrentPk2BlowfishKey();
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
        _applyingLanguage = true;
        try
        {
            PopulateThemeCombo();
            PopulateLanguageCombo();

            RefreshComboItems(_operationBox, new[] { "Encrypt", "Decrypt" });
            RefreshComboItems(_extractPayloadModeBox, new[] { "Extract restored/plain files", "Extract raw stored payload files" });
            RefreshComboItems(_importPayloadModeBox, new[] { "Store internal payloads plain", "Store internal payloads encrypted" });
        }
        finally
        {
            _applyingLanguage = false;
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
            Text = $"{T("PK2 Tools")} - {_activeProject.DisplayName}";
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
            RefreshActionState();
            Invalidate(true);
        }
        finally
        {
            _applyingLanguage = false;
        }
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
