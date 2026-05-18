using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace editor
{
    public static class LocalizationManager
    {
        private static Dictionary<string, Dictionary<string, string>> strings;
        private static string currentLanguage = "ru";
        private static Form1 mainForm;

        static LocalizationManager()
        {
            LoadStrings();
            BuildReverseTranslations();
        }

        private static void LoadStrings()
        {
            strings = new Dictionary<string, Dictionary<string, string>>();

            var ru = new Dictionary<string, string>
            {
                { "file", "Файл" },
                { "edit", "Правка" },
                { "text", "Текст" },
                { "start", "Пуск" },
                { "settings", "Настройки" },
                { "help", "Справка" },
                
                { "new", "Создать" },
                { "open", "Открыть" },
                { "save", "Сохранить" },
                { "saveAs", "Сохранить как" },
                { "exit", "Выход" },
                { "openExample1", "Открыть пример 1" },
                { "openExample2", "Открыть пример 2" },
                
                { "undo", "Отменить" },
                { "redo", "Вернуть" },
                { "cut", "Вырезать" },
                { "copy", "Копировать" },
                { "paste", "Вставить" },
                { "undoAll", "Отменить все изменения" },
                { "selectAll", "Выделить всё" },
                
                { "task", "Постановка задачи" },
                { "grammar", "Грамматика" },
                { "grammarClass", "Классификация грамматики" },
                { "analysisMethod", "Метод анализа" },
                { "testExample", "Тестовый пример" },
                { "literature", "Список литературы" },
                { "sourceCode", "Исходный код программы" },
                
                { "fontSize", "Изменение размера текста" },
                { "language", "Язык" },
                { "russian", "Русский" },
                { "english", "Английский" },
                
                { "callHelp", "Вызов справки" },
                { "about", "О программе" },
                
                { "windowTitle", "Компилятор - Редактор кода" },
                { "windowTitleEn", "Compiler - Code Editor" },
                
                { "errorColumn", "Неверный фрагмент" },
                { "locationColumn", "Местоположение" },
                { "descriptionColumn", "Описание ошибки" },
                
                { "analysisComplete", "Анализ завершен. Ошибок не обнаружено!" },
                { "errorsFound", "Анализ завершен. Найдено ошибок: {0}" },
                { "totalErrors", "Общее количество ошибок: {0}" },
                { "noErrors", "Синтаксических ошибок не обнаружено!" },
                { "noTextSelected", "Нет выделенного текста для копирования!" },
                { "clipboardEmpty", "Буфер обмена пуст или содержит не текст!" },
                { "saveChanges", "Сохранить изменения в документе '{0}'?" },
                { "unsavedChanges", "Несохраненные изменения" },
                { "saveBeforeExit", "Документ '{0}' имеет несохраненные изменения.\nСохранить перед выходом?" },
                { "cancelAllChanges", "Отменить все изменения? Это действие нельзя будет отменить." },
                { "analysisError", "Ошибка при анализе: {0}" },
                { "saveError", "Ошибка при сохранении: {0}" },
                { "openError", "Ошибка при открытии: {0}" },
                { "totalErrorsZero", "Общее количество ошибок: 0 - Синтаксических ошибок не обнаружено!" },
                { "totalErrorsCount", "Общее количество ошибок: {0}" },

                { "tooltipNew", "Создать новый документ" },
                { "tooltipOpen", "Открыть документ" },
                { "tooltipSave", "Сохранить документ" },
                { "tooltipUndoAll", "Отменить все изменения" },
                { "tooltipUndo", "Отменить изменение" },
                { "tooltipRedo", "Вернуть изменение" },
                { "tooltipCopy", "Копировать текст" },
                { "tooltipCut", "Вырезать текст" },
                { "tooltipPaste", "Вставить текст" },
                { "tooltipStart", "Пуск" },
                { "tooltipHelp", "Вызов справки" },
                { "tooltipAbout", "О программе" },
                
                { "statusReady", "Готов" },
                { "statusModified", "Изменен" },
                { "statusNoDocuments", "Нет открытых документов" },
                { "cursorPosition", "Строка: {0}, Позиция: {1}" },

                { "fontSizeEditLabel", "Размер текста в редакторе:" },
                { "fontSizeGridLabel", "Размер текста в таблице ошибок:" },
                { "apply", "Применить" },
                { "cancel", "Отмена" },

                { "errorsTab", "Ошибки" },
                { "lexemesTab", "Лексемы" },
                { "lexemeCode", "Код" },
                { "lexemeType", "Тип лексемы" },
                { "lexemeValue", "Лексема" },
                { "lexemeLocation", "Местоположение" },

                { "tokenError", "Ошибка" },
                { "tokenType_keyword", "Ключевое слово" },
                { "tokenType_id", "Идентификатор" },
                { "tokenType_integer", "Целое число" },
                { "tokenType_numeric", "Вещественное число" },
                { "tokenType_character", "Строка" },
                { "tokenType_assign", "Присваивание" },
                { "tokenType_leftparen", "Открывающая скобка" },
                { "tokenType_rightparen", "Закрывающая скобка" },
                { "tokenType_comma", "Запятая" },
                { "tokenType_minus", "Минус" },
                { "tokenType_end", "Конец оператора" },
                { "tokenType_space", "Пробел" },
                { "spaceDisplay", "(пробел)" },
            };

            var en = new Dictionary<string, string>
            {
                { "file", "File" },
                { "edit", "Edit" },
                { "text", "Text" },
                { "start", "Run" },
                { "settings", "Settings" },
                { "help", "Help" },
                
                { "new", "New" },
                { "open", "Open" },
                { "save", "Save" },
                { "saveAs", "Save As" },
                { "exit", "Exit" },
                { "openExample1", "Open example 1" },
                { "openExample2", "Open example 2" },
                
                { "undo", "Undo" },
                { "redo", "Redo" },
                { "cut", "Cut" },
                { "copy", "Copy" },
                { "paste", "Paste" },
                { "undoAll", "Undo All" },
                { "selectAll", "Select All" },
                
                { "task", "Task Statement" },
                { "grammar", "Grammar" },
                { "grammarClass", "Grammar Classification" },
                { "analysisMethod", "Analysis Method" },
                { "testExample", "Test Example" },
                { "literature", "Literature" },
                { "sourceCode", "Source Code" },
                
                { "fontSize", "Font Size" },
                { "language", "Language" },
                { "russian", "Russian" },
                { "english", "English" },
                
                { "callHelp", "Help" },
                { "about", "About" },
                
                { "windowTitle", "Compiler - Code Editor" },
                { "windowTitleEn", "Compiler - Code Editor" },
                
                { "errorColumn", "Invalid Fragment" },
                { "locationColumn", "Location" },
                { "descriptionColumn", "Error Description" },
                
                { "analysisComplete", "Analysis complete. No errors found!" },
                { "errorsFound", "Analysis complete. Errors found: {0}" },
                { "totalErrors", "Total errors: {0}" },
                { "noErrors", "No syntax errors found!" },
                { "noTextSelected", "No text selected to copy!" },
                { "clipboardEmpty", "Clipboard is empty or does not contain text!" },
                { "saveChanges", "Save changes to document '{0}'?" },
                { "unsavedChanges", "Unsaved changes" },
                { "saveBeforeExit", "Document '{0}' has unsaved changes.\nSave before exit?" },
                { "cancelAllChanges", "Undo all changes? This action cannot be undone." },
                { "analysisError", "Analysis error: {0}" },
                { "saveError", "Save error: {0}" },
                { "openError", "Open error: {0}" },
                { "totalErrorsZero", "Total errors: 0 - No syntax errors found!" },
                { "totalErrorsCount", "Total errors: {0}" },

                { "tooltipNew", "Create new document" },
                { "tooltipOpen", "Open document" },
                { "tooltipSave", "Save document" },
                { "tooltipUndoAll", "Undo all changes" },
                { "tooltipUndo", "Undo change" },
                { "tooltipRedo", "Redo change" },
                { "tooltipCopy", "Copy text" },
                { "tooltipCut", "Cut text" },
                { "tooltipPaste", "Paste text" },
                { "tooltipStart", "Run" },
                { "tooltipHelp", "Help" },
                { "tooltipAbout", "About" },
                
                { "statusReady", "Ready" },
                { "statusModified", "Modified" },
                { "statusNoDocuments", "No open documents" },
                { "cursorPosition", "Line: {0}, Position: {1}" },

                { "fontSizeEditLabel", "Text size in editor:" },
                { "fontSizeGridLabel", "Text size in error table:" },
                { "apply", "Apply" },
                { "cancel", "Cancel" },

                { "errorsTab", "Errors" },
                { "lexemesTab", "Lexemes" },
                { "lexemeCode", "Code" },
                { "lexemeType", "Lexeme Type" },
                { "lexemeValue", "Lexeme" },
                { "lexemeLocation", "Location" },

                { "tokenError", "Error" },
                { "tokenType_keyword", "Keyword" },
                { "tokenType_id", "Identifier" },
                { "tokenType_integer", "Integer" },
                { "tokenType_numeric", "Number" },
                { "tokenType_character", "String" },
                { "tokenType_assign", "Assignment" },
                { "tokenType_leftparen", "Left Parenthesis" },
                { "tokenType_rightparen", "Right Parenthesis" },
                { "tokenType_comma", "Comma" },
                { "tokenType_minus", "Minus" },
                { "tokenType_end", "End of statement" },
                { "tokenType_space", "Space" },
                { "spaceDisplay", "(space)" },
            };

            strings["ru"] = ru;
            strings["en"] = en;
        }

        private static Dictionary<string, string> errorTranslationsRuToEn = new Dictionary<string, string>
        {
            { "Незакрытая кавычка", "Unclosed quote" },
            { "Недопустимый символ", "Invalid character" },
            { "Ожидался символ '-' после '<'", "Expected '-' after '<'" },

            { "Ожидается", "Expected" },
            { "ожидается", "expected" },

            { "найдено", "found" },
            { "Найдено", "Found" },

            { "Ожидается идентификатор (буква), найдено", "Expected identifier (letter), found" },
            { "Ожидается '<-', найдено", "Expected '<-', found" },
            { "Ожидается 'c' или 'NULL', найдено", "Expected 'c' or 'NULL', found" },
            { "Ожидается '(', найдено", "Expected '(', found" },
            { "Ожидается ')', найдено", "Expected ')', found" },
            { "Ожидается число, найдено", "Expected number, found" },
            { "Ожидается ';', найдено", "Expected ';', found" },
            { "Ожидается ',' или ')', найдено", "Expected ',' or ')', found" },

            { "Ожидается идентификатор (буква)", "Expected identifier (letter)" },
            { "Ожидается '<-'", "Expected '<-'" },
            { "Ожидается 'c' или 'NULL'", "Expected 'c' or 'NULL'" },
            { "Неожиданная ')'", "Unexpected ')'" },
            { "После запятой ожидается параметр", "Expected parameter after comma" },
            { "Ожидается параметр (число, строка, TRUE, FALSE, NULL) или закрытие скобки", "Expected parameter (number, string, TRUE, FALSE, NULL) or closing parenthesis" },
            { "Ожидается число", "Expected number" },
            { "Ожидается число после '-'", "Expected number after '-'" },
            { "Ожидается ',' или ')'", "Expected ',' or ')'" },
            { "Ожидается ';'", "Expected ';'" },
            { "Неожиданный конец строки", "Unexpected end of line" },
            { "Возможно, не закрыта скобка или отсутствует ';'", "Possible unclosed parenthesis or missing ';'" },
            { "Ожидается цифра перед десятичной точкой", "Expected digit before decimal point" },
            { "Ожидается цифра после десятичной точки", "Expected digit after decimal point" },
        };

        private static Dictionary<string, string> errorTranslationsEnToRu = new Dictionary<string, string>();

        private static void BuildReverseTranslations()
        {
            foreach (var pair in errorTranslationsRuToEn)
            {
                if (!errorTranslationsEnToRu.ContainsKey(pair.Value))
                {
                    errorTranslationsEnToRu[pair.Value] = pair.Key;
                }
            }
        }

        public static string TranslateError(string error)
        {
            if (string.IsNullOrEmpty(error)) return error;

            if (currentLanguage == "ru")
            {
                string translated = error;
                var sortedReverse = errorTranslationsEnToRu
                    .OrderByDescending(x => x.Key.Length)
                    .ToList();

                foreach (var translation in sortedReverse)
                {
                    if (translated.Contains(translation.Key))
                    {
                        translated = translated.Replace(translation.Key, translation.Value);
                    }
                }
                return translated;
            }
            else
            {
                string translated = error;
                var sorted = errorTranslationsRuToEn
                    .OrderByDescending(x => x.Key.Length)
                    .ToList();

                foreach (var translation in sorted)
                {
                    if (translated.Contains(translation.Key))
                    {
                        translated = translated.Replace(translation.Key, translation.Value);
                    }
                }
                return translated;
            }
        }

        public static void Initialize(Form1 form)
        {
            mainForm = form;
        }

        public static void SetLanguage(string lang)
        {
            currentLanguage = lang;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(lang == "ru" ? "ru-RU" : "en-US");
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;

            if (mainForm != null)
            {
                mainForm.Text = GetString(lang == "ru" ? "windowTitle" : "windowTitleEn");
            }

            InfoForm.UpdateCurrentLanguage();

            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }

        public static event EventHandler LanguageChanged;

        public static string GetString(string key)
        {
            if (strings.ContainsKey(currentLanguage) && strings[currentLanguage].ContainsKey(key))
                return strings[currentLanguage][key];
            return key;
        }

        public static string FormatString(string key, params object[] args)
        {
            string format = GetString(key);
            return string.Format(format, args);
        }

        public static string CurrentLanguage => currentLanguage;

        public static DialogResult ShowMessageBox(string text, string caption, MessageBoxButtons buttons)
        {
            string translatedCaption = GetString(caption);
            return MessageBox.Show(text, translatedCaption, buttons);
        }

        public static DialogResult ShowLocalizedMessageBox(string text, string caption, MessageBoxButtons buttons)
        {
            string translatedCaption = LocalizationManager.GetString(caption);
            string translatedText = text;
            string currentLanguage = LocalizationManager.CurrentLanguage == "ru" ? "ru" : "en";

            if (currentLanguage == "en")
            {
                translatedText = LocalizationManager.TranslateError(text);
            }

            return MessageBox.Show(translatedText, translatedCaption, buttons);
        }

        public static DialogResult ShowSaveDialog(string documentName)
        {
            string caption = LocalizationManager.GetString("unsavedChanges");
            string message = LocalizationManager.FormatString("saveChanges", documentName);
            string currentLanguage = LocalizationManager.CurrentLanguage == "ru" ? "ru" : "en";


            if (currentLanguage == "en")
            {
                using (var form = new Form())
                {
                    form.Text = caption;
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.FormBorderStyle = FormBorderStyle.FixedDialog;
                    form.MaximizeBox = false;
                    form.MinimizeBox = false;
                    form.Size = new Size(400, 150);

                    Label label = new Label();
                    label.Text = message;
                    label.Location = new Point(20, 20);
                    label.Size = new Size(350, 50);
                    label.TextAlign = ContentAlignment.MiddleLeft;

                    Button btnYes = new Button();
                    btnYes.Text = "Yes";
                    btnYes.Location = new Point(180, 80);
                    btnYes.Size = new Size(60, 30);
                    btnYes.DialogResult = DialogResult.Yes;

                    Button btnNo = new Button();
                    btnNo.Text = "No";
                    btnNo.Location = new Point(250, 80);
                    btnNo.Size = new Size(60, 30);
                    btnNo.DialogResult = DialogResult.No;

                    Button btnCancel = new Button();
                    btnCancel.Text = "Cancel";
                    btnCancel.Location = new Point(320, 80);
                    btnCancel.Size = new Size(60, 30);
                    btnCancel.DialogResult = DialogResult.Cancel;

                    form.Controls.Add(label);
                    form.Controls.Add(btnYes);
                    form.Controls.Add(btnNo);
                    form.Controls.Add(btnCancel);

                    return form.ShowDialog();
                }
            }
            else
            {
                return MessageBox.Show(message, caption, MessageBoxButtons.YesNoCancel);
            }
        }
    }
}