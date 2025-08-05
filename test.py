import time
import threading
import keyboard
import win32gui
import win32con

spamming = False

# Remplace par le nom exact de la fenêtre (visible dans la barre de titre)
TARGET_WINDOW_NAME = "Roblox"

def toggle_spam():
    global spamming
    spamming = not spamming
    state = "ON" if spamming else "OFF"
    print(f"[INFO] Spam mode: {state}")

def find_window(title):
    return win32gui.FindWindow(None, title)

def send_key(hwnd, key_code):
    # Simule un appui sur une touche
    win32gui.PostMessage(hwnd, win32con.WM_KEYDOWN, key_code, 0)
    time.sleep(0.01)
    win32gui.PostMessage(hwnd, win32con.WM_KEYUP, key_code, 0)

def spam_to_window():
    hwnd = find_window(TARGET_WINDOW_NAME)
    if hwnd == 0:
        print(f"[ERROR] Fenêtre '{TARGET_WINDOW_NAME}' non trouvée.")
        return

    while True:
        if spamming:
            send_key(hwnd, ord('E'))  # Envoie la touche 'E'
            time.sleep(0.05)
        else:
            time.sleep(0.1)

keyboard.add_hotkey('y', toggle_spam)

threading.Thread(target=spam_to_window, daemon=True).start()

print("Le script est prêt. Appuie sur 'y' pour activer/désactiver le spam dans la fenêtre cible.")
print("Appuie sur 'esc' pour quitter.")
keyboard.wait('esc')
