# Prepare public-domain paintings: crop scan borders, convert to sRGB, resize, save JPG.
import sys, os, io
from PIL import Image, ImageCms, ImageFilter
Image.MAX_IMAGE_PIXELS = None
SRC = sys.argv[1]; DST = sys.argv[2]
# (source file, output name, crop fractions l,t,r,b, long side)
JOBS = [
 ("Fantin-Latour,_Henri_-_Roses_and_Nasturtiums_in_a_Vase.jpg", "Art_FantinLatour_RosesAndNasturtiums", (0.008,0.008,0.012,0.008), 2048),
 ("Heda,_Willem_Claesz_-_Breakfast_with_a_Lobster.jpg", "Art_Heda_BreakfastWithLobster", (0.016,0.012,0.012,0.037), 2048),
 ("Pereda,_Antonio_de_-_Still_Life_with_an_Ebony_Chest.jpg", "Art_Pereda_StillLifeEbonyChest", (0.006,0.006,0.006,0.006), 2048),
 ("Pissarro,_Camille_-_Boulevard_Monmartre_in_Paris.jpg", "Art_Pissarro_BoulevardMontmartre", (0.008,0.008,0.008,0.008), 2048),
 ("Monet,_Claude_-_Waterloo_Bridge._Effect_of_Fog.jpg", "Art_Monet_WaterlooBridgeFog", (0.008,0.008,0.008,0.008), 2048),
 ("Gellee,_Claude_(Le_Lorrain)_-_Morning_in_the_Harbour.jpg", "Art_Lorrain_MorningInTheHarbour", (0.014,0.014,0.014,0.014), 2048),
 ("Aivazovsky,_Ivan_-_The_Ninth_Wave.jpg", "Art_Aivazovsky_NinthWave", (0.006,0.006,0.006,0.006), 2048),
 ("Bellotto,_Bernardo_-_Neumarkt_in_Dresden.jpg", "Art_Bellotto_NeumarktDresden", (0.006,0.008,0.006,0.008), 2048),
 ("Robert,_Hubert_-_Landscape_with_a_Waterfall.jpg", "Art_Robert_LandscapeWithWaterfall", (0.012,0.008,0.012,0.008), 2048),
]
srgb = ImageCms.createProfile("sRGB")
for src, name, (l,t,r,b), longside in JOBS:
    im = Image.open(os.path.join(SRC, src))
    icc = im.info.get("icc_profile")
    if im.mode == "CMYK":
        if icc:
            im = ImageCms.profileToProfile(im, ImageCms.ImageCmsProfile(io.BytesIO(icc)), srgb, outputMode="RGB", renderingIntent=ImageCms.Intent.PERCEPTUAL)
        else:
            im = im.convert("RGB")
    else:
        im = im.convert("RGB")
    w, h = im.size
    im = im.crop((int(w*l), int(h*t), int(w*(1-r)), int(h*(1-b))))
    w, h = im.size
    s = longside / max(w, h)
    nw, nh = round(w*s), round(h*s)
    nw -= nw % 4; nh -= nh % 4
    im = im.resize((nw, nh), Image.LANCZOS)
    im = im.filter(ImageFilter.UnsharpMask(radius=1.0, percent=40, threshold=2))
    out = os.path.join(DST, name + ".jpg")
    im.save(out, "JPEG", quality=86, optimize=True, progressive=False, subsampling="4:2:0")
    print(name, im.size, os.path.getsize(out))
