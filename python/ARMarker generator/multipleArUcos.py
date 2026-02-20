# Generate N ArUco markers at a fixed printed size with thin cut borders.
# Automatically paginates and arranges markers to fit the chosen page size.
# Saves PNGs + multi-page PDF next to this .py file.

import os
import math
import cv2
import numpy as np
from PIL import Image
from reportlab.pdfgen import canvas
from reportlab.lib.units import cm
from reportlab.lib.utils import ImageReader
from reportlab.lib import pagesizes

# -----------------------------
# User parameters (edit these)
# -----------------------------
num_markers      = 12                # total markers to generate
ARUCO_DICT_NAME  = "DICT_4X4_50"     # e.g., DICT_4X4_50, DICT_6X6_250, etc.
START_ID         = 0                 # first ID to generate

marker_cm        = 6.0               # exact printed size of the marker (black square)
trim_cm          = 0.2               # extra white around marker (for cutting), per side
dpi              = 300               # image DPI for PNGs

page_size_name   = "A4"              # "A4", "A3", "LETTER", "LEGAL" ...
margins_cm       = (1.0, 1.0, 1.0, 1.0)  # (left, right, top, bottom) outer page margins

cutline_color    = (0.7, 0.7, 0.7)   # RGB 0..1 for the thin border lines on paper
cutline_width_pt = 0.5               # thin cut border line width in points (~1/72 inch)

label_text       = True             # set True to print small "ID n" under each marker

# -----------------------------
# Derived sizing
# -----------------------------
output_dir = os.path.dirname(os.path.abspath(__file__))
os.makedirs(output_dir, exist_ok=True)

marker_px = int(round(marker_cm / 2.54 * dpi))  # cm -> inches -> px
# We'll render a white border into the PNG so it visually exists even without the PDF frame
# (this helps if someone prints the PNGs directly).
png_white_border_px = max(1, int(round((trim_cm / 2.54) * dpi)))

# Cell size on the page (in physical units):
cell_w_cm = marker_cm + 2 * trim_cm
cell_h_cm = marker_cm + 2 * trim_cm

# Page size lookup
PAGE_SIZES = {
    "A0": pagesizes.A0, "A1": pagesizes.A1, "A2": pagesizes.A2, "A3": pagesizes.A3,
    "A4": pagesizes.A4, "A5": pagesizes.A5, "LETTER": pagesizes.LETTER, "LEGAL": pagesizes.LEGAL,
    "TABLOID": pagesizes.TABLOID
}
if page_size_name.upper() not in PAGE_SIZES:
    raise ValueError(f"Unknown page size '{page_size_name}'. Choose one of: {', '.join(PAGE_SIZES.keys())}")
page_width_pt, page_height_pt = PAGE_SIZES[page_size_name.upper()]

# Convert sizes to points (ReportLab units)
cell_w_pt = cell_w_cm * cm
cell_h_pt = cell_h_cm * cm
left_margin_pt, right_margin_pt, top_margin_pt, bottom_margin_pt = [m * cm for m in margins_cm]

usable_w_pt = page_width_pt  - (left_margin_pt + right_margin_pt)
usable_h_pt = page_height_pt - (top_margin_pt  + bottom_margin_pt)

# Compute how many fit per page
cols = max(1, int(usable_w_pt // cell_w_pt))
rows = max(1, int(usable_h_pt // cell_h_pt))
per_page = cols * rows
total_pages = math.ceil(num_markers / per_page) if per_page > 0 else 1

# -----------------------------
# OpenCV ArUco utilities
# -----------------------------
# Map string name -> cv2.aruco constant
DICT_MAP = {name: getattr(cv2.aruco, name) for name in dir(cv2.aruco) if name.startswith("DICT_")}
if ARUCO_DICT_NAME not in DICT_MAP:
    raise ValueError(f"Unknown ArUco dictionary: {ARUCO_DICT_NAME}")
aruco_dict = cv2.aruco.getPredefinedDictionary(DICT_MAP[ARUCO_DICT_NAME])

# Pick function name compatible with your OpenCV
def draw_aruco_marker(dictionary, marker_id, side_px):
    if hasattr(cv2.aruco, "drawMarker"):
        return cv2.aruco.drawMarker(dictionary, marker_id, side_px)
    elif hasattr(cv2.aruco, "generateImageMarker"):
        return cv2.aruco.generateImageMarker(dictionary, marker_id, side_px)
    else:
        raise AttributeError("Your OpenCV build lacks both drawMarker and generateImageMarker. "
                             "Try: pip install opencv-contrib-python==4.9.0.80")

# Pillow resampling (keep edges crisp)
try:
    RESAMPLE_NEAREST = Image.Resampling.NEAREST
except AttributeError:
    RESAMPLE_NEAREST = Image.NEAREST

# -----------------------------
# Generate marker PNGs
# -----------------------------
image_paths = []
for i in range(num_markers):
    marker_id = START_ID + i
    # Create the black/white marker at final black-square size (marker_px)
    marker_img = draw_aruco_marker(aruco_dict, marker_id, marker_px)  # uint8, 0/255
    # Add a white border (trim) around the marker inside the PNG
    bordered = cv2.copyMakeBorder(
        marker_img, png_white_border_px, png_white_border_px,
        png_white_border_px, png_white_border_px,
        borderType=cv2.BORDER_CONSTANT, value=255
    )
    # Resize back to the exact final PNG dimensions so the printed black area remains marker_cm
    # and the white border is thin (visual reference). To keep the black square exactly marker_px
    # on paper, we render the PNG at (marker_px + 2*border) then scale back to marker_px.
    pil_img = Image.fromarray(bordered)
    pil_img = pil_img.resize((marker_px, marker_px), RESAMPLE_NEAREST)
    pil_img = pil_img.convert("RGB")
    pil_img.info["dpi"] = (dpi, dpi)

    filename = f"Aruco_{ARUCO_DICT_NAME}_id{marker_id:02d}.png"
    path = os.path.join(output_dir, filename)
    pil_img.save(path, dpi=(dpi, dpi))
    image_paths.append(path)

# -----------------------------
# Create multi-page PDF
# -----------------------------
pdf_path = os.path.join(output_dir, f"Aruco_{ARUCO_DICT_NAME}_{marker_cm:.1f}cm_{page_size_name}.pdf")
c = canvas.Canvas(pdf_path, pagesize=(page_width_pt, page_height_pt))
c.setTitle(f"ArUco {ARUCO_DICT_NAME} - {marker_cm:.1f}cm - {page_size_name}")

def draw_page(page_index, paths_slice):
    # Center grid within usable area
    grid_w_pt = cols * cell_w_pt
    grid_h_pt = rows * cell_h_pt
    # Position grid inside margins and centered
    left = left_margin_pt + (usable_w_pt - grid_w_pt) / 2
    bottom = bottom_margin_pt + (usable_h_pt - grid_h_pt) / 2

    c.setLineWidth(cutline_width_pt)
    c.setStrokeColorRGB(*cutline_color)

    for r in range(rows):
        for col in range(cols):
            idx = r * cols + col
            if idx >= len(paths_slice):
                return
            x = left + col * cell_w_pt
            y = bottom + (rows - 1 - r) * cell_h_pt  # top-to-bottom placement

            # Thin cut rectangle around the cell
            c.rect(x, y, cell_w_pt, cell_h_pt, stroke=1, fill=0)

            # Place marker centered in the cell (marker occupies marker_cm; trim is outside)
            img = Image.open(paths_slice[idx])
            img_reader = ImageReader(img)
            # draw the image at size = marker_cm; center within the cell (which is marker_cm + 2*trim)
            marker_w_pt = marker_cm * cm
            marker_h_pt = marker_cm * cm
            img_x = x + (cell_w_pt - marker_w_pt) / 2
            img_y = y + (cell_h_pt - marker_h_pt) / 2
            c.drawImage(img_reader, img_x, img_y, width=marker_w_pt, height=marker_h_pt,
                        preserveAspectRatio=True, mask='auto')

            if label_text:
                c.setFont("Helvetica", 4)
                base = os.path.basename(paths_slice[idx])
                if base.lower().endswith(".png"):
                    base = base[:-4]
                c.drawCentredString(x + cell_w_pt / 2, y+4, base)  # small label under the cell

# paginate
for p in range(total_pages):
    start = p * per_page
    end = min((p + 1) * per_page, num_markers)
    draw_page(p, image_paths[start:end])
    c.showPage()

c.save()

print(f"PDF saved to: {pdf_path}")
print(f"Page size: {page_size_name}  |  grid: {cols} x {rows}  |  pages: {total_pages}")
