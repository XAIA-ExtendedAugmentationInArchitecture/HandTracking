import os
import math
from PIL import Image
import qrcode

from reportlab.pdfgen import canvas
from reportlab.lib.pagesizes import A4, LETTER, LEGAL, TABLOID
from reportlab.lib.units import cm
from reportlab.lib.utils import ImageReader
from reportlab.pdfbase.pdfmetrics import stringWidth


# ----------------------------
# USER SETTINGS
# ----------------------------
num_codes = 30
codes_data = [f"QRCode_{i}" for i in range(num_codes)]  # replace with real payloads

qr_cm = 15                 # <-- printed QR image size (INCLUDING quiet zone)
gap_cm = 1.5                # <-- space between QR edge and outline, all around
dpi = 300

paper_size_name = "A4"      # "A4", "LETTER", "LEGAL", "TABLOID"
pdf_filename = f"QR_codes_{num_codes}_{qr_cm}_{paper_size_name}.pdf"

outline_stroke_pt = 1.0     # outline thickness
min_page_margin_cm = 0.8    # outer page margin

# QR settings
qr_error = qrcode.constants.ERROR_CORRECT_L
qr_border_modules = 0       # quiet zone in modules

# Version policys
PREFERRED_MAX_VERSION = 10
HARD_MAX_VERSION = 20


# ----------------------------
# HELPERS
# ----------------------------
def get_pagesize(name: str):
    name = (name or "").strip().upper()
    sizes = {"A4": A4, "LETTER": LETTER, "LEGAL": LEGAL, "TABLOID": TABLOID}
    if name in sizes:
        return sizes[name]
    raise ValueError(f"Unsupported paper_size_name='{name}'. Use A4, LETTER, LEGAL, TABLOID.")


try:
    RESAMPLE_NEAREST = Image.Resampling.NEAREST
except AttributeError:
    RESAMPLE_NEAREST = Image.NEAREST


def best_fit_font_size(text, max_width_pt, max_height_pt, font_name="Helvetica", start=12, min_size=6):
    for size in range(start, min_size - 1, -1):
        if stringWidth(text, font_name, size) <= max_width_pt and size <= max_height_pt:
            return size
    return min_size


def make_qr_png_fixed_print_size(data: str, target_px: int):
    """Generate QR using minimal version, enforce version caps, then resize to exact target_px."""
    qr = qrcode.QRCode(
        version=None,
        error_correction=qr_error,
        box_size=10,           # temporary; will resize
        border=qr_border_modules
    )
    qr.add_data(data)
    qr.make(fit=True)
    ver = qr.version

    if ver > HARD_MAX_VERSION:
        raise ValueError(f"Payload needs QR version {ver} (> {HARD_MAX_VERSION}). Shorten payload.")
    if ver > PREFERRED_MAX_VERSION:
        raise ValueError(f"Payload needs QR version {ver} (> {PREFERRED_MAX_VERSION}). Shorten payload.")

    img = qr.make_image(fill_color="black", back_color="white").convert("RGB")
    img = img.resize((target_px, target_px), RESAMPLE_NEAREST)
    img.info["dpi"] = (dpi, dpi)
    return img, ver


# ----------------------------
# MAIN
# ----------------------------
def main():
    output_dir = os.path.dirname(os.path.abspath(__file__))
    os.makedirs(output_dir, exist_ok=True)

    if len(codes_data) < num_codes:
        raise ValueError(f"codes_data has {len(codes_data)} items but num_codes={num_codes}.")

    # Fixed QR PNG pixel size (so print size is constant)
    qr_px = int(round(qr_cm / 2.54 * dpi))

    # Generate PNGs
    image_paths = []
    version_report = []
    for i in range(0, num_codes):
        img, ver = make_qr_png_fixed_print_size(codes_data[i], qr_px)
        filename = f"QRCode_{i:02d}.png"
        path = os.path.join(output_dir, filename)
        img.save(path, dpi=(dpi, dpi))
        image_paths.append(path)
        version_report.append((filename, ver))

    # Layout
    page_w, page_h = get_pagesize(paper_size_name)

    qr_pt = qr_cm * cm
    gap_pt = gap_cm * cm
    cell_pt = qr_pt + 2 * gap_pt  # outline square size

    margin_pt = min_page_margin_cm * cm
    usable_w = page_w - 2 * margin_pt
    usable_h = page_h - 2 * margin_pt

    cols = int(usable_w // cell_pt)
    rows = int(usable_h // cell_pt)
    if cols < 1 or rows < 1:
        raise ValueError(
            f"Cannot fit even 1 item on {paper_size_name}.\n"
            f"Cell size: {cell_pt/cm:.2f} cm, usable area: {usable_w/cm:.2f} x {usable_h/cm:.2f} cm."
        )

    per_page = cols * rows
    pages = math.ceil(num_codes / per_page)

    # Center grid
    grid_w = cols * cell_pt
    grid_h = rows * cell_pt
    left = (page_w - grid_w) / 2
    bottom = (page_h - grid_h) / 2

    pdf_path = os.path.join(output_dir, pdf_filename)
    c = canvas.Canvas(pdf_path, pagesize=(page_w, page_h))
    c.setTitle("QR Codes - Auto Layout")
    c.setLineWidth(outline_stroke_pt)

    idx = 0
    for _ in range(pages):
        for r in range(rows):
            for col in range(cols):
                if idx >= num_codes:
                    break

                x = left + col * cell_pt
                y = bottom + (rows - 1 - r) * cell_pt  # top-to-bottom

                # Outline
                c.rect(x, y, cell_pt, cell_pt, stroke=1, fill=0)

                # QR placed with exact gap all around
                qr_x = x + gap_pt
                qr_y = y + gap_pt
                c.drawImage(
                    ImageReader(Image.open(image_paths[idx])),
                    qr_x, qr_y,
                    width=qr_pt, height=qr_pt,
                    mask="auto"
                )

                # Label: put it inside bottom gap band (optional)
                label = f"QRCode_{idx:02d}"
                band_h = gap_pt  # bottom gap height
                inner_pad = 2
                max_w = cell_pt - 2 * inner_pad
                max_h = band_h - 2 * inner_pad

                if max_h > 6:
                    font_size = best_fit_font_size(label, max_w, max_h, start=12, min_size=6)
                    c.setFont("Helvetica", font_size)
                    c.drawCentredString(x + cell_pt/2, y + (band_h/2) - (font_size * 0.35), label)

                idx += 1

            if idx >= num_codes:
                break

        c.showPage()

    c.save()

    print(f"Paper: {paper_size_name} | Grid: {cols}x{rows} | Per page: {per_page} | Pages: {pages}")
    print(f"PDF saved to: {pdf_path}")
    print("QR version summary:")
    for name, v in version_report:
        print(f"  {name}: version {v}")
    print("\nIMPORTANT: To verify physical cm sizes, print/export at 100% (Actual Size), not 'Fit'.")


if __name__ == "__main__":
    main()
