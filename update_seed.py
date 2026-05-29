import sys
import re

file_path = "DataWarehouse_Scripts/Seed_Realistic_Data.sql"

with open(file_path, "r", encoding="utf-8") as f:
    lines = f.readlines()

in_order_details = False
in_cart_items = False

def process_order_details(line):
    # Regex to match a tuple: (int, int, int, int, float)
    def replacer(match):
        p_id = int(match.group(3))
        size_id = p_id * 3 - 1
        return f"({match.group(1)}, {match.group(2)}, {size_id}, {match.group(4)}, {match.group(5)})"
    return re.sub(r'\((\d+),\s*(\d+),\s*(\d+),\s*(\d+),\s*([\d\.]+)\)', replacer, line)

def process_cart_items(line):
    # Regex to match a tuple: (int, int, int, int)
    def replacer(match):
        p_id = int(match.group(3))
        size_id = p_id * 3 - 1
        return f"({match.group(1)}, {match.group(2)}, {size_id}, {match.group(4)})"
    return re.sub(r'\((\d+),\s*(\d+),\s*(\d+),\s*(\d+)\)', replacer, line)

for i in range(len(lines)):
    if "INSERT INTO [OrderDetails]" in lines[i]:
        in_order_details = True
        lines[i] = lines[i].replace("[ProductID]", "[SizeID]")
        continue
    if "SET IDENTITY_INSERT [OrderDetails] OFF" in lines[i]:
        in_order_details = False
        continue
        
    if "INSERT INTO [CartItems]" in lines[i]:
        in_cart_items = True
        lines[i] = lines[i].replace("[ProductID]", "[SizeID]")
        continue
    if "SET IDENTITY_INSERT [CartItems] OFF" in lines[i]:
        in_cart_items = False
        continue

    if in_order_details and lines[i].strip().startswith('('):
        lines[i] = process_order_details(lines[i])
        
    if in_cart_items and lines[i].strip().startswith('('):
        lines[i] = process_cart_items(lines[i])

with open(file_path, "w", encoding="utf-8") as f:
    f.writelines(lines)
print("Updated successfully.")
