"""
download_missing_items.py
Compares items.xml (itempower>0) against the local ITEMS\ folder,
then downloads any missing item images from the Albion Online Render API.

Usage:
    python download_missing_items.py              # download all missing
    python download_missing_items.py --crystal    # download only missing crystal items
    python download_missing_items.py --dry-run    # list what would be downloaded

API endpoint:
    https://render.albiononline.com/v1/item/{uniquename}.png
"""

import xml.etree.ElementTree as ET
import os
import sys
import urllib.request
import urllib.error
import time
import concurrent.futures

# --- Config ---
ITEMS_XML = "ao-bin-dumps/items.xml"
ITEMS_DIR = "ITEMS"
RENDER_API = "https://render.albiononline.com/v1/item/{name}.png"
IMAGE_SIZE = 217          # max size supported by the API
MAX_WORKERS = 8           # concurrent downloads
RETRY_COUNT = 3
RETRY_DELAY = 2           # seconds between retries


def get_all_item_names(xml_path):
    """Parse items.xml and return list of all uniquenames with itempower > 0 (incl enchantments)."""
    tree = ET.parse(xml_path)
    root = tree.getroot()
    names = []
    for item in root:
        uname = item.get("uniquename")
        if not uname:
            continue
        ip = int(item.get("itempower", "0"))
        if ip > 0:
            names.append(uname)
        for enc in item.findall("./enchantments/enchantment"):
            eip = int(enc.get("itempower", "0"))
            if eip > 0:
                names.append(uname + "@" + enc.get("enchantmentlevel", "0"))
    return names


def find_missing(all_names, items_dir):
    """Return list of names whose .png does not exist in items_dir."""
    existing = set(os.listdir(items_dir))
    return [n for n in all_names if (n + ".png") not in existing]


def download_one(name):
    """Download a single item image. Returns (name, success, message)."""
    url = RENDER_API.format(name=name) + f"?size={IMAGE_SIZE}"
    dest = os.path.join(ITEMS_DIR, name + ".png")

    for attempt in range(1, RETRY_COUNT + 1):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
            with urllib.request.urlopen(req, timeout=15) as resp:
                data = resp.read()
            # Sanity: PNG files start with the 8-byte PNG signature
            if len(data) < 8 or data[:4] != b"\x89PNG":
                return (name, False, f"Invalid PNG response ({len(data)} bytes)")
            with open(dest, "wb") as f:
                f.write(data)
            return (name, True, f"{len(data)} bytes")
        except urllib.error.HTTPError as e:
            if e.code == 404:
                return (name, False, "404 Not Found on server")
            if attempt < RETRY_COUNT:
                time.sleep(RETRY_DELAY)
                continue
            return (name, False, f"HTTP {e.code}")
        except Exception as e:
            if attempt < RETRY_COUNT:
                time.sleep(RETRY_DELAY)
                continue
            return (name, False, str(e))
    return (name, False, "All retries exhausted")


def main():
    crystal_only = "--crystal" in sys.argv
    dry_run = "--dry-run" in sys.argv

    if not os.path.exists(ITEMS_XML):
        print(f"[ERROR] {ITEMS_XML} not found. Run from the project root directory.")
        sys.exit(1)
    if not os.path.exists(ITEMS_DIR):
        os.makedirs(ITEMS_DIR)
        print(f"[INFO] Created {ITEMS_DIR}/ directory")

    print("[INFO] Parsing items.xml ...")
    all_names = get_all_item_names(ITEMS_XML)
    missing = find_missing(all_names, ITEMS_DIR)

    if crystal_only:
        missing = [n for n in missing if "CRYSTAL" in n.upper()]
        print(f"[INFO] Filtering to crystal items only")

    print(f"[INFO] Total items in XML (itempower>0): {len(all_names)}")
    print(f"[INFO] Already downloaded: {len(all_names) - len(find_missing(all_names, ITEMS_DIR))}")
    print(f"[INFO] Missing to download: {len(missing)}")

    if not missing:
        print("[OK] All items already downloaded!")
        return

    if dry_run:
        print("\n[DRY-RUN] Would download:")
        for n in missing:
            print(f"  {n}")
        return

    print(f"[INFO] Starting download with {MAX_WORKERS} workers ...\n")

    success = 0
    failed = 0
    not_found = 0
    errors = []

    with concurrent.futures.ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
        futures = {executor.submit(download_one, name): name for name in missing}
        for i, future in enumerate(concurrent.futures.as_completed(futures), 1):
            name, ok, msg = future.result()
            if ok:
                success += 1
                if success % 50 == 0:
                    print(f"  [{success}/{len(missing)}] downloaded ...")
            else:
                if "404" in msg:
                    not_found += 1
                else:
                    failed += 1
                    errors.append((name, msg))

    print(f"\n[DONE] Results:")
    print(f"  Success : {success}")
    print(f"  404 (not on server): {not_found}")
    print(f"  Failed  : {failed}")

    if errors:
        print(f"\n[ERRORS] ({len(errors)} items):")
        for name, msg in errors[:20]:
            print(f"  {name}: {msg}")
        if len(errors) > 20:
            print(f"  ... and {len(errors) - 20} more")


if __name__ == "__main__":
    main()
