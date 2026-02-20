import qrcode
import os

def generate_qr_code(data, filename):
    # Create QR code instance
    qr = qrcode.QRCode(
        version=2,
        error_correction=qrcode.constants.ERROR_CORRECT_L,
        box_size=25,
        border=4,
    )
    
    # Add data to QR code
    qr.add_data(data)
    qr.make(fit=True)

    # Create an image from the QR code instance
    img = qr.make_image(fill_color="black", back_color="white")
    script_dir = os.path.dirname(os.path.abspath(__file__))
    filepath = os.path.join(script_dir, filename)

    # Save the image
    img.save(filepath)


if __name__ == "__main__":
    # Example usage
    for i in range(0, 5):
        data = f"QRCode_{i}"
        filename = f"QR_{i}.png" 
        generate_qr_code(data, filename)
        print(f"QR code saved as {filename}")
