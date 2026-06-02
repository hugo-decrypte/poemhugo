# Dépendances requises pour ce script :     //changed:22/05
# - Python 3.10+
# - beautifulsoup4 : pip install beautifulsoup4
# - pymupdf        : pip install pymupdf
# - Pillow         : pip install Pillow 
#
# Modèle Ollama requis : glm-ocr:latest (2.2 Go) (nommé ainsi à la date : 20/05/2026)
#   Installation : ollama pull glm-ocr
#   Ollama doit tourner en arrière-plan sur http://localhost:11434 : "D:\Program Files\Ollama\ollama.exe" serve
#
# Lancement : D:\mon_env\Scripts\python.exe extract_table_glmocr.py <fichier_image>

import sys
import base64
import json
import urllib.request
import urllib.error
from pathlib import Path
from bs4 import BeautifulSoup   # beautifulsoup4
import fitz     # pymupdf
from PIL import Image   # Pillow
import io
 
OLLAMA_URL = "http://localhost:11434/api/generate"
MODEL = "glm-ocr:latest"
 
IMAGE_SUFFIXES = {'.png', '.jpg', '.jpeg', '.bmp', '.tif', '.tiff', '.gif'}

PDF_SUFFIX = '.pdf' 

PROMPT = """Extract the table from this image as HTML. Return only the HTML table, nothing else."""

PROMPT_TITLE = """What is the title of this document? Reply with only the title, nothing else."""

if len(sys.argv) >= 3:
    KEYWORDS = set(sys.argv[2].split(","))
else:
    KEYWORDS = {"Commande", "Client", "Adresse", "Ville", "Date commande", "Date livraison", "#Produit", "#Palette", "Quantité", "Commentaire"} 

def image_to_base64(file_path: str) -> str:
    suffix = Path(file_path).suffix.lower()
    if suffix in {'.tif', '.tiff', '.gif'}:   # Ollama ne gère pas ces formats donc on les convertit en PNG en mémoire
        img = Image.open(file_path)
        buf = io.BytesIO()
        img.save(buf, format='PNG')
        return base64.b64encode(buf.getvalue()).decode('utf-8')
    with open(file_path, 'rb') as f:
        return base64.b64encode(f.read()).decode('utf-8')

def pdf_page_to_base64(page) -> str:
    """Convertit une page PDF en image PNG base64."""
    mat = fitz.Matrix(1.25, 1.25)  
    pix = page.get_pixmap(matrix=mat)
    #pix.save("C:/Users/jiand/Desktop/debug_page1.png")
    png_bytes = pix.tobytes("png")
    return base64.b64encode(png_bytes).decode('utf-8')
 
def call_ollama(image_b64: str, prompt : str) -> str:
    payload = {
        "model": MODEL,
        "prompt": prompt,
        "images": [image_b64],
        "stream": True,
        "options": {
            "num_ctx": 8192,
            "num_predict": 8192
        }
    }
    data = json.dumps(payload).encode('utf-8')
    req = urllib.request.Request(
        OLLAMA_URL,
        data=data,
        headers={"Content-Type": "application/json"},
        method="POST"
    )
    full_response = []
    try:
        with urllib.request.urlopen(req, timeout=300) as resp:
            for line in resp:
                line = line.decode('utf-8').strip()
                if not line:
                    continue
                chunk = json.loads(line)
                token = chunk.get("response", "")
                full_response.append(token)
        return "".join(full_response).strip()
    except urllib.error.URLError as e:
        print(f"\nERREUR : impossible de contacter Ollama : {e}", file=sys.stderr)
        sys.exit(1)
 
def extract_data_rows(html: str, nb_cols: int) -> list[list[str]]:
    """Extrait uniquement les lignes de données depuis toutes les tables du HTML."""
    soup = BeautifulSoup(html, 'html.parser')
    rows = []
    for tr in soup.find_all('tr'):
        # Prendre th ET td
        cells = [c.get_text(strip=True) for c in tr.find_all(['th', 'td'])]
        if not cells:
            continue
        # Ignorer si c'est une ligne d'en-tête (contient des keywords)
        score = sum(1 for c in cells if any(k.lower() in c.lower() for k in KEYWORDS))
        if score >= 2:
            continue
        # Ignorer les lignes trop courtes
        if len(cells) < 2:
            continue
        # Aligner sur nb_cols
        filtered = [cells[i] if i < len(cells) else '' for i in range(nb_cols)]
        rows.append(filtered)
    return rows
 
def parse_html_table(html: str) -> tuple[list[str], list[list[str]]]:
    soup = BeautifulSoup(html, 'html.parser')

    # Trouver la ligne d'en-tête avec le plus de keywords
    header_row = None
    best_score = 0
    all_rows = soup.find_all('tr')
    for tr in all_rows:
        cells = [c.get_text(strip=True) for c in tr.find_all(['th', 'td'])]
        score = sum(1 for c in cells if any(k.lower() in c.lower() for k in KEYWORDS))
        if score > best_score:
            best_score = score
            header_row = tr

    if header_row is None:
        return [], []

    # Extraire les en-têtes et leurs indices (ignorer ceux sans texte)
    all_headers = [c.get_text(strip=True) for c in header_row.find_all(['th', 'td'])]
    valid_indices = [i for i, h in enumerate(all_headers) if h]
    headers = [all_headers[i] for i in valid_indices]

    # Extraire les lignes de données après la ligne d'en-tête
    header_idx = all_rows.index(header_row)
    rows = []
    for tr in all_rows[header_idx + 1:]:
        cells = [td.get_text(strip=True) for td in tr.find_all(['th', 'td'])]
        if cells:
            # Garder uniquement les colonnes avec en-tête valide
            filtered = [cells[i] if i < len(cells) else '' for i in valid_indices]
            rows.append(filtered)

    return headers, rows
 
 
def main():
    if len(sys.argv) < 2:
        print("Usage: extract_table_glmocr.py <fichier_image>", file=sys.stderr)
        sys.exit(1)
 
    file_path = sys.argv[1]
    suffix = Path(file_path).suffix.lower()
    
    if len(sys.argv) >= 3:
        KEYWORDS.clear()
        KEYWORDS.update(sys.argv[2].split(","))

    if suffix == PDF_SUFFIX:
        # Traitement PDF multi-pages
        doc = fitz.open(file_path)
        nb_pages = len(doc)
 
        # Page 1 : titre + en-têtes + données
        page1_b64 = pdf_page_to_base64(doc[0])
        title = call_ollama(page1_b64, PROMPT_TITLE)
        print("=== ZONE EN-TETE ===")
        print(title)
 
        html_page1 = call_ollama(page1_b64, PROMPT)
        headers, rows = parse_html_table(html_page1)

        if not headers:
            print("ERREUR : aucun en-tête détecté dans le tableau.", file=sys.stderr)
            sys.exit(1)

        # Pages suivantes
        for i in range(1, nb_pages):
            page_b64 = pdf_page_to_base64(doc[i])
            html_page = call_ollama(page_b64, PROMPT)
            extra_rows = extract_data_rows(html_page, len(headers))
            rows.extend(extra_rows)

        doc.close()

    elif suffix in IMAGE_SUFFIXES:
        image_b64 = image_to_base64(file_path)
        title = call_ollama(image_b64, PROMPT_TITLE)
        print(f"=== ZONE EN-TETE ===")
        print(title)

        html = call_ollama(image_b64, PROMPT)
    
        headers, rows = parse_html_table(html)

        if not headers:
            print("ERREUR : aucun en-tête détecté dans le tableau.", file=sys.stderr)
            sys.exit(1)
    
    else:
        print(f"ERREUR : extension non supportée : {suffix}", file=sys.stderr)
        sys.exit(1)

    print("=== ZONE TABLEAU ===")
    for row in rows:
        parts = [f"[{headers[i] if i < len(headers) else f'Col{i+1}'}: {row[i]}]" for i in range(len(row)) if row[i].strip()]
        if not any(cell.strip().lower() == "total" for cell in row):
            print(" ".join(parts))
 
 
if __name__ == "__main__":
    main()