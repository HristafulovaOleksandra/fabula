from pathlib import Path

import pandas as pd
from openpyxl import Workbook, load_workbook
from openpyxl.styles import Alignment, Font, PatternFill
from openpyxl.worksheet.datavalidation import DataValidation

ANNOTATION_LEVELS = ["A1", "A2", "B1", "B2", "C1", "C2"]
LEVEL_TO_ANNOTATOR_NUMBER = {level: index + 1 for index, level in enumerate(ANNOTATION_LEVELS)}
SHEET_NAME = "Розмітка"
HEADERS = ["item_id", "№", "Попередня репліка (контекст)", "Репліка", "Рівень", "Не впевнена", "Коментар"]
COLUMN_WIDTHS = {"A": 10, "B": 6, "C": 55, "D": 70, "E": 10, "F": 13, "G": 35}

INSTRUCTIONS = [
    "Інструкція до розмітки реплік за CEFR",
    "",
    "Що оцінювати: рівень CEFR, на якому учень зможе зрозуміти саму репліку (колонка «Репліка») під час читання.",
    "Попередня репліка показана лише як контекст, її рівень не оцінюється.",
    "",
    "Як оцінювати:",
    "1. Спирайся на власний досвід викладання і на опис рівнів CEFR для читання (can-do).",
    "2. Зважай разом на лексику, граматику і загальну зрозумілість. Не став рівень лише за найскладнішим словом.",
    "3. Можна користуватися словником, щоб перевірити рівень слова.",
    "4. Обирай рівень зі списку: A1, A2, B1, B2, C1, C2.",
    "5. Якщо вагаєшся між двома рівнями, обери той, який здається ближчим, і постав «так» у колонці «Не впевнена».",
    "6. Коментар необов'язковий: наприклад, якщо репліка незрозуміла без ширшого контексту або містить помилку.",
    "",
    "Не повертайся до вже розмічених реплік і не змінюй оцінки після завершення.",
    "Не дивись прогнози моделей до кінця розмітки.",
    "",
    "Орієнтири рівнів для читання (коротко):",
    "A1 — дуже прості фрази про себе й найближче оточення, найчастіші слова.",
    "A2 — короткі прості тексти на побутові теми, базова граматика.",
    "B1 — зрозумілий текст на знайомі теми, поширена лексика, складніші речення.",
    "B2 — розгорнутий текст, абстрактні теми, різноманітна лексика і граматика.",
    "C1 — довгі складні тексти, імпліцитні значення, ідіоми.",
    "C2 — практично будь-який текст, рідкісна лексика, тонкі стилістичні відтінки.",
]


def write_annotation_workbook(items: pd.DataFrame, path: Path, title_note: str = "") -> None:
    workbook = Workbook()
    sheet = workbook.active
    sheet.title = SHEET_NAME
    sheet.append(HEADERS)
    for number, row in enumerate(items.itertuples(index=False), start=1):
        sheet.append([row.item_id, number, row.context, row.utterance, None, None, None])

    header_fill = PatternFill("solid", fgColor="2F3E9E")
    for cell in sheet[1]:
        cell.font = Font(bold=True, color="FFFFFF")
        cell.fill = header_fill
        cell.alignment = Alignment(vertical="center", wrap_text=True)
    for column, width in COLUMN_WIDTHS.items():
        sheet.column_dimensions[column].width = width
    sheet.column_dimensions["A"].hidden = True
    for row in sheet.iter_rows(min_row=2):
        for cell in row:
            cell.alignment = Alignment(vertical="top", wrap_text=True)
        row[2].font = Font(italic=True, color="6B6B6B")
        row[3].font = Font(size=12)
    sheet.freeze_panes = "C2"

    level_validation = DataValidation(type="list", formula1='"' + ",".join(ANNOTATION_LEVELS) + '"', allow_blank=True)
    unsure_validation = DataValidation(type="list", formula1='"так"', allow_blank=True)
    sheet.add_data_validation(level_validation)
    sheet.add_data_validation(unsure_validation)
    last_row = len(items) + 1
    level_validation.add(f"E2:E{last_row}")
    unsure_validation.add(f"F2:F{last_row}")

    guide = workbook.create_sheet("Інструкція")
    lines = INSTRUCTIONS + ([""] + [title_note] if title_note else [])
    for line in lines:
        guide.append([line])
    guide.column_dimensions["A"].width = 120
    guide["A1"].font = Font(bold=True, size=14)
    workbook.save(path)


def read_annotation_workbook(path: Path) -> pd.DataFrame:
    sheet = load_workbook(path, data_only=True)[SHEET_NAME]
    rows = list(sheet.iter_rows(min_row=2, values_only=True))
    frame = pd.DataFrame(rows, columns=HEADERS)
    frame = frame.rename(columns={"Рівень": "level", "Не впевнена": "unsure", "Коментар": "comment"})
    frame["level"] = frame["level"].astype("string").str.strip().str.upper()
    frame["unsure"] = frame["unsure"].astype("string").str.strip().str.lower().eq("так")
    return frame[["item_id", "level", "unsure", "comment"]]
