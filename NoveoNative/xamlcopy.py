import os
import shutil

# Get current directory
current_dir = os.getcwd()

# Get Desktop path
desktop = os.path.join(os.path.expanduser("~"), "Desktop")

# Loop through all files
for file in os.listdir(current_dir):
    if file.lower().endswith(".xaml"):
        xaml_path = os.path.join(current_dir, file)
        txt_name = file + ".txt"
        txt_path = os.path.join(current_dir, txt_name)

        # Copy content to .txt
        with open(xaml_path, "r", encoding="utf-8", errors="ignore") as f_src:
            with open(txt_path, "w", encoding="utf-8") as f_dst:
                f_dst.write(f_src.read())

        # Move to Desktop
        shutil.move(txt_path, os.path.join(desktop, txt_name))

print("✅ All .xaml files copied to .txt and moved to Desktop.")
