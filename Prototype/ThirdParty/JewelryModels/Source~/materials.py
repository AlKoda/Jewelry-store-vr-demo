"""Material library: OBJ/MTL values (Kd = linear-ish albedo for Unity's OBJ import, which reads Kd as the colour)
and recommended Built-in Standard shader settings (materials.json in the pack)."""
MATS = {
 # metals: albedo = specular colour of the metal (Standard metallic workflow)
 'Gold18k':      dict(Kd=(1.00, 0.78, 0.40), metallic=1.0, smoothness=0.90, note='18k yellow gold'),
 'GoldRose':     dict(Kd=(0.97, 0.66, 0.55), metallic=1.0, smoothness=0.88, note='18k rose gold'),
 'GoldFine':     dict(Kd=(1.00, 0.77, 0.34), metallic=1.0, smoothness=0.82, note='999.9 fine gold (bars, coins)'),
 'GoldBrushed':  dict(Kd=(1.00, 0.77, 0.38), metallic=1.0, smoothness=0.62, note='satin gold'),
 'Platinum':     dict(Kd=(0.83, 0.82, 0.80), metallic=1.0, smoothness=0.90, note='platinum / white gold (rhodium plated)'),
 'Silver925':    dict(Kd=(0.97, 0.96, 0.94), metallic=1.0, smoothness=0.86, note='sterling silver'),
 'SteelPolished':dict(Kd=(0.78, 0.78, 0.78), metallic=1.0, smoothness=0.88, note='polished stainless steel'),
 'SteelBrushed': dict(Kd=(0.72, 0.72, 0.72), metallic=1.0, smoothness=0.55, note='brushed steel'),
 'Chrome':       dict(Kd=(0.80, 0.80, 0.82), metallic=1.0, smoothness=0.95, note='chrome'),
 'Brass':        dict(Kd=(0.90, 0.73, 0.42), metallic=1.0, smoothness=0.75, note='polished brass'),
 'BlackMetal':   dict(Kd=(0.09, 0.09, 0.10), metallic=0.8, smoothness=0.55, note='black anodised / painted metal'),
 # gems: transparent in Built-in only via Fade/Transparent; a cubemap-reflection shader sells them best
 'Diamond':      dict(Kd=(0.96, 0.97, 1.00), d=0.35, metallic=0.25, smoothness=0.98, mode='Transparent', note='brilliant; add reflection probe / sparkle shader'),
 'Sapphire':     dict(Kd=(0.05, 0.12, 0.55), d=0.70, metallic=0.20, smoothness=0.97, mode='Transparent', note='blue sapphire'),
 'Ruby':         dict(Kd=(0.62, 0.02, 0.08), d=0.70, metallic=0.20, smoothness=0.97, mode='Transparent', note='ruby'),
 'Emerald':      dict(Kd=(0.02, 0.45, 0.18), d=0.70, metallic=0.20, smoothness=0.96, mode='Transparent', note='emerald'),
 'Pearl':        dict(Kd=(0.93, 0.90, 0.84), metallic=0.10, smoothness=0.80, note='akoya pearl, slight pink-cream'),
 'Glass':        dict(Kd=(0.90, 0.94, 0.95), d=0.20, metallic=0.0, smoothness=0.97, mode='Transparent', note='crystal / lens glass'),
 'Acrylic':      dict(Kd=(0.95, 0.97, 0.98), d=0.25, metallic=0.0, smoothness=0.95, mode='Transparent', note='clear acrylic'),
 # soft goods
 'VelvetBlack':  dict(Kd=(0.035, 0.035, 0.040), metallic=0.0, smoothness=0.18, note='black velvet'),
 'VelvetNavy':   dict(Kd=(0.030, 0.045, 0.110), metallic=0.0, smoothness=0.18, note='navy velvet'),
 'VelvetCream':  dict(Kd=(0.78, 0.72, 0.62), metallic=0.0, smoothness=0.15, note='cream suede / velvet'),
 'VelvetCharcoal':dict(Kd=(0.11, 0.11, 0.12), metallic=0.0, smoothness=0.18, note='charcoal velvet (matches StoreKit)'),
 'LinenGrey':    dict(Kd=(0.55, 0.53, 0.50), metallic=0.0, smoothness=0.12, note='linen display cover'),
 'LeatherBrown': dict(Kd=(0.20, 0.09, 0.04), metallic=0.0, smoothness=0.42, note='calf leather strap'),
 'LeatherBlack': dict(Kd=(0.035, 0.032, 0.03), metallic=0.0, smoothness=0.45, note='black leather'),
 'Walnut':       dict(Kd=(0.26, 0.15, 0.08), metallic=0.0, smoothness=0.55, note='lacquered walnut'),
 'PaperWhite':   dict(Kd=(0.92, 0.91, 0.88), metallic=0.0, smoothness=0.20, note='card stock'),
 'PaperBlue':    dict(Kd=(0.62, 0.74, 0.86), metallic=0.0, smoothness=0.20, note='diamond-paper liner'),
 'SatinRibbon':  dict(Kd=(0.70, 0.56, 0.30), metallic=0.0, smoothness=0.62, note='champagne satin ribbon'),
 'Cord':         dict(Kd=(0.85, 0.80, 0.70), metallic=0.0, smoothness=0.15, note='cotton string / rope; also the silk knots of the pearl strands'),
 'PlasticBlack': dict(Kd=(0.035, 0.035, 0.038), metallic=0.0, smoothness=0.55, note='black ABS'),
 'PlasticGrey':  dict(Kd=(0.30, 0.31, 0.33), metallic=0.0, smoothness=0.45, note='grey ABS'),
 'RubberBlack':  dict(Kd=(0.03, 0.03, 0.03), metallic=0.0, smoothness=0.25, note='rubber'),
 'Screen':       dict(Kd=(1.0, 1.0, 1.0), metallic=0.0, smoothness=0.92, emission=0.6, note='display glass; texture is emissive'),
}

def srgb(c):
    return tuple(round(float(x) ** (1 / 2.2), 4) for x in c)

def mtl(name, tex=None):
    """MTL values: Kd is written in sRGB (what Unity's colour fields expect); MATS keeps linear albedo."""
    d = dict(MATS[name]); kd = srgb(d['Kd'])
    out = dict(Kd=kd, Ks=(0.5, 0.5, 0.5) if d.get('metallic', 0) > 0.5 else (0.2, 0.2, 0.2),
               Ns=round(10 + 990 * d.get('smoothness', 0.5) ** 2, 1), d=d.get('d', 1.0))
    if tex: out['map_Kd'] = tex; out['Kd'] = (1.0, 1.0, 1.0)
    return out
