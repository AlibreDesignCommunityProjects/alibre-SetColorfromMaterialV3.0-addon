"""
Script Name: Set Color from Material
Author: Cator
Version: 2.0
Description: This script allows you to set a color for each material chosen from the Alibre Material library.
For each material in the library, a color must be added following the RGB scheme like example.
Once the material is selected and the script is run,
it will apply the color set in the script with just a few clicks. 
Version 2.0: Added all standard materials from the Alibre library and the materials attached by Harold for Keyshot. 
The script has been restructured with a more functional and concise dictionary. 
An error message has been added if the material is not found in the libraries. 
Version 3.0: Enter the choice of material. Choose the material and the RGB color set in the script is set.
"""

Win = Windows()

Enviroment = CurrentPart()

# Updates the user interface based on the current selections made
def UpdateUserInterface():
    for i in range(len(Options)):
        if i not in DoNotDisable:
            pass

# Dictionary to map materials to RGB colors
material_colors = {
    "Beryllium": (114, 114, 255),
    "Gold - Pure": (225, 237, 36),
    "Wood - Mahogony": (128, 64, 0),
    "Aluminium": (223, 223, 223),
    "Brass": (237, 220, 135),
    "Chrome": (247, 247, 247),
    "Copper": (249, 185, 142),
    "Gold 14K": (232, 215, 156),
    "Gold 18K": (232, 210, 143),
    "Gold 24K": (233, 204, 113),
    "Nickel": (244, 242, 217),
    "Platinum": (254, 254, 255),
    "Silver": (226, 221, 218),
    "Stainless Steel": (229, 238, 236),
    "Steel": (201, 196, 193),
    "Titanium": (194, 189, 173),
    "Carbon-Carbon Composite": (60, 60, 60),
    "Cobalt": (70, 130, 180),
    "Graphite": (50, 50, 50),
    "Iridium": (180, 180, 180),
    "Lead": (80, 80, 80),
    "Leather - common": (139, 69, 19),
    "Magnesium - Pure": (232, 232, 232),
    "Manganese": (50, 50, 50),
    "Molybdenum - wrought": (100, 100, 100),
    "Money Metal - rolled": (220, 220, 220),
    "Paraffin": (240, 240, 240),
    "Phenolic Resin": (139, 69, 19),
    "Platinum - cast-hammered": (200, 200, 200),
    "Polyurethane": (255, 204, 0),
    "Rubber": (0, 0, 0),
    "Sapphire": (8, 37, 103),
    "Tin - cast-hammered": (200, 200, 200),
    "Tantalum": (90, 90, 90),
    "Zinc - Cast": (200, 200, 200),
    "Aluminum - cast-hammered": (185, 185, 185),
    "Aluminum, 2024-T3": (160, 160, 160),
    "Aluminum, 6061-T6": (175, 175, 175),
    "Aluminum, 7079-T6": (170, 170, 170),
    "Welded Aluminum": (180, 180, 180),
    "Brass - cast-rolled": (184, 134, 11),
    "Welded Brass": (222, 184, 135),
    "Bronze": (205, 127, 50),
    "Bronze - aluminum": (200, 175, 120),
    "Bronze - phosphor": (255, 219, 88),
    "Bronze - ~11% Tin": (186, 104, 50),
    "Copper Alloy": (150, 90, 30),
    "Glass": (255, 255, 255),
    "Glass, lead": (220, 220, 220),
    "Quartz Glass": (255, 255, 255),
    "Gold Coin (US)": (255, 215, 0),
    "Iron": (139, 137, 137),
    "Iron - Cast - Pig": (112, 128, 144),
    "Iron - Ductile": (150, 150, 150),
    "Iron - Ferrosilicon": (100, 100, 100),
    "Iron - Spiegeleisen": (120, 120, 120),
    "Iron - grey cast": (140, 140, 140),
    "Iron - wrought": (140, 140, 140),
    "Pure Iron (99.9% Fe)": (75, 75, 75),
    "Kevlar": (255, 255, 0),
    "Kevlar 149": (255, 255, 0),
    "Kevlar 29": (255, 255, 0),
    "Kevlar 49": (255, 255, 0),
    "Plastic": (255, 255, 255),
    "ABS": (255, 255, 255),
    "ABS/PC": (220, 220, 220),
    "Acetal Resin": (255, 255, 255),
    "Nylon 6/10": (240, 240, 240),
    "PA Type 6": (255, 255, 255),
    "PBT General Purpose": (255, 255, 255),
    "PC High Viscosity": (240, 240, 240),
    "PE High Density": (200, 200, 200),
    "PE Low/Medium Density": (190, 190, 190),
    "POM Acetal Copolymer": (240, 240, 240),
    "PP Copolymer": (240, 240, 240),
    "PS Medium/High": (240, 240, 240),
    "PTFE (General)": (240, 240, 240),
    "PVC Rigid": (240, 240, 240),
    "Polyaryletherketone Resin": (230, 230, 230),
    "UHMW": (240, 240, 240),
    "Silicon": (100, 100, 100),
    "Silicone": (230, 230, 230),
    "Silicon Nitride": (200, 200, 200),
    "German Silver": (200, 200, 200),
    "Silver - cast-hammered": (192, 192, 192),
    "Silver - Pure": (192, 192, 192),
    "Alloy Steel": (170, 170, 170),
    "Carbon Steel(0.40% C)": (150, 150, 150),
    "Carbon Tool Steel(0.90% C)": (140, 140, 140),
    "Cast Steel": (130, 130, 130),
    "High Speed Tool Steel(18% W)": (120, 120, 120),
    "Galvanized Steel": (170, 170, 170),
    "Soft Steel (0.06% C)": (150, 150, 150),
    "Stainless steel (17Cr-.12C)": (230, 230, 230),
    "Stainless steel (27Cr-.35C)": (230, 230, 230),
    "Stainless Steel - 304": (230, 230, 230),
    "Steel - cold-drawn": (120, 120, 120),
    "Steel - tool": (120, 120, 120),
    "Welded Stainless Steel (440C)": (230, 230, 230),
    "Wrought Steel": (130, 130, 130),
    "Tungsten": (85, 85, 85),
    "Tungsten Carbide": (150, 150, 150),
    "Uranium": (160, 160, 160),
    "Uranium D38": (160, 160, 160),
    "Vanadium": (160, 160, 160),
    "Vanadium Carbide": (110, 110, 110),
    "Wood - Birch": (222, 184, 135),
    "Wood - Cherry": (220, 85, 45),
    "Wood - Red Oak": (155, 95, 52),
    "Wood - Southern Pine": (224, 198, 150),
    "Wood - Sugar Maple": (223, 194, 133),
    "Wood - Walnut": (138, 69, 56),
    "Zirconium": (160, 160, 160),
    "Zirconia": (230, 230, 230),
    "Zirconium Carbide": (110, 110, 110)
}

material_list = sorted(material_colors.keys())  # Sort the material list alphabetically
# Windows 
DoNotDisable = []
Options = [
    ['Choose Material from List', WindowsInputTypes.StringList, material_list],
]
Values = Win.OptionsDialog("Set Material and Color", Options, 180, None, UpdateUserInterface)
if Values == None:
  sys.exit()

# If the value of Values is an index, retrieve the corresponding material
if isinstance(Values[0], int) and 0 <= Values[0] < len(material_list):
    selected_material = material_list[Values[0]]  # Get the material from the list
else:
    selected_material = None  # Handle the case where the value is not valid

if selected_material:
    # Set the material in the environment
    Enviroment.Material = selected_material  # Use the selected material

    # Set the color based on the material
    if selected_material in material_colors:
        Enviroment.SetColor(*material_colors[selected_material])
    else:
        # Default color for unknown materials
        Enviroment.SetColor(255, 0, 0)  # Red for unknown material
        Win.InfoDialog("Choose an existing material or update the script.", "Unknown Material")
else:
    print("Invalid material selection.")
