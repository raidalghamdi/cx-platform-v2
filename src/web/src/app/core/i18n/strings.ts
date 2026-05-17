// Single source of bilingual strings. Server returns EN+AR side-by-side
// for content, so this file only carries UI chrome and labels.

export type Lang = 'en' | 'ar';

export const STRINGS: Record<string, Record<Lang, string>> = {
  // Brand / shell
  'brand.name':            { en: 'GAC Customer Experience', ar: 'تجربة المستفيد — الهيئة' },
  'brand.short':           { en: 'GAC CX', ar: 'الهيئة' },
  'brand.tagline':         { en: 'Customer experience — managed by GAC, for GAC.', ar: 'تجربة المستفيد — تُدار من الهيئة، للهيئة.' },
  'brand.poweredBy':       { en: 'GAC — General Authority for Competition', ar: 'الهيئة العامة للمنافسة' },

  // Nav
  'nav.dashboard':         { en: 'Dashboard', ar: 'لوحة المؤشرات' },
  'nav.complaints':        { en: 'Complaints', ar: 'الشكاوى' },
  'nav.inbox':             { en: 'Inbox', ar: 'الصندوق' },
  'nav.admin':             { en: 'Admin', ar: 'الإدارة' },
  'nav.logout':            { en: 'Sign out', ar: 'تسجيل الخروج' },

  // Common
  'common.email':          { en: 'Email', ar: 'البريد الإلكتروني' },
  'common.password':       { en: 'Password', ar: 'كلمة المرور' },
  'common.signIn':         { en: 'Sign in', ar: 'تسجيل الدخول' },
  'common.all':            { en: 'All', ar: 'الكل' },
  'common.save':           { en: 'Save', ar: 'حفظ' },
  'common.cancel':         { en: 'Cancel', ar: 'إلغاء' },
  'common.send':           { en: 'Send', ar: 'إرسال' },
  'common.close':          { en: 'Close', ar: 'إغلاق' },
  'common.empty':          { en: 'No records to display', ar: 'لا توجد سجلات للعرض' },
  'common.loading':        { en: 'Loading…', ar: 'جارٍ التحميل…' },
  'common.openDrawer':     { en: 'Open', ar: 'فتح' },
  'common.actions':        { en: 'Actions', ar: 'إجراءات' },
  'common.optional':       { en: 'optional', ar: 'اختياري' },
  'common.toggleLang':     { en: 'العربية', ar: 'English' },
  'common.refresh':        { en: 'Refresh', ar: 'تحديث' },

  // Login
  'login.title':           { en: 'Sign in to your workspace', ar: 'تسجيل الدخول إلى مساحة عملك' },
  'login.subtitle':        { en: 'Use a demo account to explore the system.', ar: 'استخدم حساباً تجريبياً لاستكشاف النظام.' },
  'login.demoTitle':       { en: 'Demo accounts', ar: 'حسابات تجريبية' },
  'login.demoHint':        { en: 'Tap a row to fill in the form. Password is demo.', ar: 'اضغط أي صف لتعبئة النموذج. كلمة المرور: demo' },
  'login.demoFooter':      { en: 'Demo accounts visible during pilot — hidden in production.', ar: 'حسابات تجريبية تظهر خلال التجريب — تُخفى في الإنتاج.' },
  'login.invalid':         { en: 'Invalid email or password', ar: 'بريد إلكتروني أو كلمة مرور غير صحيحة' },

  // Roles
  'role.admin':            { en: 'Administrator', ar: 'مسؤول النظام' },
  'role.supervisor':       { en: 'Supervisor', ar: 'مشرف' },
  'role.agent':            { en: 'Agent', ar: 'موظف خدمة' },
  'role.quality':          { en: 'Quality officer', ar: 'مسؤول الجودة' },
  'role.customer':         { en: 'Customer', ar: 'مستفيد' },
  'role.executive':        { en: 'Executive', ar: 'تنفيذي' },

  // Dashboard
  'dashboard.title':       { en: 'Executive scorecard', ar: 'لوحة المؤشرات التنفيذية' },
  'dashboard.kpiSource':   { en: 'Source: Strategic KPIs Excel', ar: 'المصدر: ملف مؤشرات الاستراتيجية' },
  'dashboard.catSource':   { en: 'Source: Monafasah+ (API)', ar: 'المصدر: منافسة+ (API)' },
  'dashboard.byCategory':  { en: 'Complaints by category', ar: 'الشكاوى حسب الفئة' },

  // Complaints
  'complaints.title':      { en: 'Complaints', ar: 'الشكاوى' },
  'complaints.tab.all':    { en: 'All', ar: 'الكل' },
  'complaints.tab.down':   { en: 'Down Journey Focus', ar: 'تركيز الرحلة المتعثرة' },
  'complaints.code':       { en: 'Code', ar: 'المرجع' },
  'complaints.subject':    { en: 'Subject', ar: 'الموضوع' },
  'complaints.category':   { en: 'Category', ar: 'الفئة' },
  'complaints.priority':   { en: 'Priority', ar: 'الأولوية' },
  'complaints.status':     { en: 'Status', ar: 'الحالة' },
  'complaints.opened':     { en: 'Opened', ar: 'تاريخ الفتح' },
  'complaints.closed':     { en: 'Close Date', ar: 'تاريخ الإغلاق' },
  'complaints.stage':      { en: 'Journey Stage', ar: 'مرحلة الرحلة' },
  'complaints.body':       { en: 'Description', ar: 'الوصف' },
  'complaints.resolve':    { en: 'Resolve', ar: 'إغلاق الحالة' },
  'complaints.reopen':     { en: 'Reopen', ar: 'إعادة فتح' },
  'complaints.markInProgress': { en: 'Mark in progress', ar: 'قيد التنفيذ' },
  'complaints.addNote':    { en: 'Add note', ar: 'إضافة ملاحظة' },
  'complaints.notePlaceholder': { en: 'Add an internal note…', ar: 'أضف ملاحظة داخلية…' },
  'complaints.statusChanged': { en: 'Status updated', ar: 'تم تحديث الحالة' },
  'complaints.noteSaved':  { en: 'Note saved', ar: 'تم حفظ الملاحظة' },
  'complaints.downBanner': { en: 'These complaints occurred during a downward step in the customer journey and need focused remediation.', ar: 'وقعت هذه الشكاوى أثناء خطوة متعثّرة من رحلة المستفيد وتتطلب معالجة مركّزة.' },

  // Inbox
  'inbox.title':           { en: 'Agent inbox', ar: 'صندوق الموظف' },
  'inbox.banner':          { en: 'Threads aggregated from email, WhatsApp Business, and the Contact Us form. Configure channels under Admin.', ar: 'تُجمَّع الرسائل من البريد وواتساب للأعمال ونموذج «تواصل معنا». اضبط القنوات من الإدارة.' },
  'inbox.channel.all':     { en: 'All channels', ar: 'كل القنوات' },
  'inbox.channel.Email':   { en: 'Email', ar: 'البريد' },
  'inbox.channel.WhatsApp':{ en: 'WhatsApp', ar: 'واتساب' },
  'inbox.channel.Chat':    { en: 'Chat', ar: 'محادثة' },
  'inbox.status.New':      { en: 'New', ar: 'جديد' },
  'inbox.status.Open':     { en: 'Open', ar: 'مفتوح' },
  'inbox.status.Replied':  { en: 'Replied', ar: 'تم الرد' },
  'inbox.status.Closed':   { en: 'Closed', ar: 'مغلق' },
  'inbox.reply.subject':   { en: 'Subject', ar: 'الموضوع' },
  'inbox.reply.body':      { en: 'Body', ar: 'النص' },
  'inbox.reply.send':      { en: 'Send reply', ar: 'إرسال الرد' },
  'inbox.reply.sending':   { en: 'Sending…', ar: 'جارٍ الإرسال…' },
  'inbox.reply.sent':      { en: 'Reply sent', ar: 'تم إرسال الرد' },
  'inbox.reply.failed':    { en: 'Send failed', ar: 'فشل الإرسال' },
  'inbox.reply.your':      { en: 'Your reply', ar: 'ردك' },
  'inbox.original':        { en: 'Original message', ar: 'الرسالة الأصلية' },
  'inbox.charCount':       { en: 'characters', ar: 'حرفاً' },

  // Admin
  'admin.title':           { en: 'Admin', ar: 'الإدارة' },
  'admin.tab.roles':       { en: 'Role permissions', ar: 'صلاحيات الأدوار' },
  'admin.tab.channels':    { en: 'Contact channels', ar: 'قنوات التواصل' },
  'admin.role.intro':      { en: 'Choose which pages each role can access. The admin row is always fully allowed.', ar: 'اختر الصفحات التي يمكن لكل دور الوصول إليها. مسؤول النظام دائماً مفعّل.' },
  'admin.channels.whatsapp': { en: 'WhatsApp number', ar: 'رقم واتساب' },
  'admin.channels.info_email': { en: 'Info email', ar: 'البريد الرسمي' },
  'admin.channels.support_hours': { en: 'Support hours', ar: 'ساعات الدعم' },
  'admin.saved':           { en: 'Saved', ar: 'تم الحفظ' },
};
