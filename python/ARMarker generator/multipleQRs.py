# Generate 12 QR codes (6x6 cm) and lay them out on an A4 PDF with cut borders
# Saves PNGs and the PDF in the SAME folder as this .py file.
import os
from PIL import Image
import qrcode
from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import cm
from reportlab.lib.utils import ImageReader

# --- Settings ---
num_codes = 30
codes_data = [f"QR_{i+1}" for i in range(num_codes)]  # <- replace with your real payloads
output_dir = os.path.dirname(os.path.abspath(__file__))
os.makedirs(output_dir, exist_ok=True)

# Size settings
qr_cm = 6.0                 # physical size of each QR code in centimeters
dpi = 300                   # print resolution
qr_px = int(round(qr_cm / 2.54 * dpi))  # convert cm to inches to pixels

# QR code configuration
# We'll let the library choose the minimal version needed (version=None),
# then enforce your version policy.
qr_error = qrcode.constants.ERROR_CORRECT_L
qr_border_modules = 4       # quiet zone: 4 modules

# Version policy
PREFERRED_MAX_VERSION = 10  # aim to stay ≤ 10
HARD_MAX_VERSION = 20       # never allow > 20

# Layout settings for A4
page_width, page_height = A4  # in points (1 point = 1/72 inch)
cols = 3
rows = 4
cell_w = 6 * cm
cell_h = 6 * cm

# Compute margins to center the grid
grid_w = cols * cell_w
grid_h = rows * cell_h
left_margin = (page_width - grid_w) / 2
bottom_margin = (page_height - grid_h) / 2

# Pillow resampling (compat with older/newer Pillow)
try:
    RESAMPLE_NEAREST = Image.Resampling.NEAREST  # Pillow >= 9.1
except AttributeError:
    RESAMPLE_NEAREST = Image.NEAREST             # older Pillow

def make_qr_image_and_check_version(data, error_correction, border_modules, dpi, target_px):
    """Create a QR image using the minimal version, then enforce version caps."""
    qr = qrcode.QRCode(
        version=None,              # auto-pick minimal version
        error_correction=error_correction,
        box_size=10,               # temporary; we'll resize to exact size
        border=border_modules,
    )
    qr.add_data(data)
    qr.make(fit=True)
    selected_version = qr.version

    # Enforce limits
    if selected_version > HARD_MAX_VERSION:
        raise ValueError(
            f"Payload requires QR version {selected_version} (> {HARD_MAX_VERSION}) which is unsupported. "
            "Shorten the data (use a shorter URL, encode an ID, or remove extra parameters)."
        )
    if selected_version > PREFERRED_MAX_VERSION:
        raise ValueError(
            f"Payload requires QR version {selected_version} (> {PREFERRED_MAX_VERSION}) which is not guaranteed. "
            "Shorten the data to improve tracking reliability."
        )

    img = qr.make_image(fill_color="black", back_color="white").convert("RGB")
    # Resize to exact physical size at target DPI without smoothing (keeps crisp modules)
    img = img.resize((target_px, target_px), RESAMPLE_NEAREST)
    img.info["dpi"] = (dpi, dpi)
    return img, selected_version

# --- Generate individual 6x6 cm PNG QR codes with version checks ---
image_paths = []
version_report = []
for idx, data in enumerate(codes_data, start=1):
    img, ver = make_qr_image_and_check_version(
        data=data,
        error_correction=qr_error,
        border_modules=qr_border_modules,
        dpi=dpi,
        target_px=qr_px
    )
    filename = f"QR_{idx:02d}.png"
    path = os.path.join(output_dir, filename)
    img.save(path, dpi=(dpi, dpi))
    image_paths.append(path)
    version_report.append((filename, ver))

# --- Create A4 PDF with 12 QR codes and cut borders ---
pdf_path = os.path.join(output_dir, "QR_codes_A4_3x4_6cm.pdf")
c = canvas.Canvas(pdf_path, pagesize=A4)
c.setTitle("QR Codes - 6x6cm - A4 layout")

# Draw light trim lines (cut borders) around each 6x6 cm cell and place images
c.setLineWidth(0.5)  # thin lines
for r in range(rows):
    for col in range(cols):
        idx = r * cols + col
        if idx >= len(image_paths):
            break

        x = left_margin + col * cell_w
        y = bottom_margin + (rows - 1 - r) * cell_h  # top-to-bottom placement

        # Trim rectangle (cut guide)
        c.rect(x, y, cell_w, cell_h, stroke=1, fill=0)

        # Center the QR inside the cell (exactly 6x6 cm)
        pil_img = Image.open(image_paths[idx])
        img_reader = ImageReader(pil_img)
        c.drawImage(img_reader, x, y, width=cell_w, height=cell_h,
                    preserveAspectRatio=True, mask='auto')

c.showPage()
c.save()

# --- Console summary ---
print(f"PDF saved to: {pdf_path}")
print("QR version summary (lower = less dense = easier to track):")
for name, v in version_report:
    print(f"  {name}: version {v}")
