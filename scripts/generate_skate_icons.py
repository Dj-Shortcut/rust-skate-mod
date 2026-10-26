#!/usr/bin/env python3
"""CC0 original skatepark silhouettes; no imported images, models, paths or fonts.
Requires Pillow. python generate_skate_icons.py --output skate_icons
"""
import argparse, json, math
from pathlib import Path
from PIL import Image, ImageDraw, ImageCms

def rect(x,y,w,h): return [(x,y),(x+w,y),(x+w,y+h),(x,y+h)]
def circle(x,y,r): return [(x+r*math.cos(i*math.tau/32),y+r*math.sin(i*math.tau/32)) for i in range(32)]
def curve(x0,y0,x1,y1):
    # Original quarter ellipse: a horizontal bottom tangent and a vertical upper lip.
    return [(x0+(x1-x0)*math.sin(t),y0-(y0-y1)*(1-math.cos(t))) for t in [i*math.pi/2/24 for i in range(25)]]
def qp(x0,x1,lip,back,base=216):
    return curve(x0,base-8,x1,lip)+[(back,lip),(back,base),(x0,base)]
def hp(x0,x1,y,base=216):
    mid=(x0+x1)/2; gap=(x1-x0)*.13
    left=[(x0,y),(x0+16,y)]+list(reversed(curve(mid-gap,base-12,x0+16,y)))+[(mid+gap,base-12)]+curve(mid+gap,base-12,x1-16,y)[1:]+[(x1,y),(x1,base),(x0,base)]
    return left

def catalogue():
    return [
      ('bank', [[(32,216),(224,216),(224,175.2)], []]),
      ('kicker', [[(43,216),(43,208),(95,201),(139,181),(172,150),(192,113),(214,113),(214,216)]]),
      ('quarterpipe',[qp(34,183,59,222),circle(183,59,6)]),
      ('ledge-low',[rect(51,164,154,52),rect(51,156,154,8)]),
      ('ledge-high',[rect(66,115,124,101),rect(62,107,132,8)]),
      ('long-ledge',[rect(27,177,202,39),rect(27,169,202,8)]),
      ('funbox',[[(28,216),(28,208),(74,130),(180,130),(228,208),(228,216)],rect(75,122,105,8)]),
      ('halfpipe',[hp(30,226,82),circle(46,82,6),circle(210,82,6)]),
      ('mini-quarterpipe',[qp(41,153,119,210),circle(153,119,6)]),
      ('large-quarterpipe',[qp(32,177,64,225),circle(177,64,6),rect(193,29,7,36),rect(218,29,7,36),rect(193,29,32,7)]),
      ('half-wall-kicker',[[(33,216),(33,208),(85,201),(132,177),(162,139),(176,97),(207,97),(207,216)],rect(202,82,22,134)]),
      ('mega-halfpipe',[hp(27,229,73),circle(43,73,6),circle(213,73,6),rect(27,29,7,44),rect(52,29,7,44),rect(27,29,32,7),rect(197,29,7,44),rect(222,29,7,44),rect(197,29,32,7)])
    ]

def dpath(polys):
    return ' '.join('M '+' L '.join(f'{x:.3f} {y:.3f}' for x,y in poly)+' Z' for poly in polys if poly)
def mask(polys,size):
    k=size*4/256; m=Image.new('L',(size*4,size*4)); d=ImageDraw.Draw(m)
    for p in polys:
        if p:d.polygon([(round(x*k),round(y*k)) for x,y in p],fill=255)
    return m.resize((size,size),Image.Resampling.LANCZOS)
def tint(mask,color):
    im=Image.new('RGBA',mask.size,(*color,255)); im.putalpha(mask);return im

def main():
    ap=argparse.ArgumentParser();ap.add_argument('--output',required=True);args=ap.parse_args();out=Path(args.output);out.mkdir(parents=True,exist_ok=True)
    profile=ImageCms.ImageCmsProfile(ImageCms.createProfile('sRGB')).tobytes(); report=[]; icons=[]
    for sector,(name,polys) in enumerate(catalogue()):
        assert len(polys)>0
        svg=f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256"><path fill="#FFFFFF" d="{dpath(polys)}"/></svg>\n'
        (out/f'skate_icon_{name}.svg').write_text(svg)
        for size in [128,256]:
            m=mask(polys,size);im=tint(m,(255,255,255));im.save(out/f'skate_icon_{name}_{size}.png',icc_profile=profile)
            box=m.point(lambda p:255 if p>=128 else 0).getbbox();assert box and box[0]>=size*.10 and box[1]>=size*.10 and box[2]<=size*.90 and box[3]<=size*.90,(name,size,box)
            assert all(r==g==b==255 for r,g,b,a in im.getdata()),name
        icons.append(mask(polys,110));report.append({'sector':sector,'name':name,'bounds256':mask(polys,256).getbbox(),'minimum_authored_detail_256px':6})
    # Preview is separate UI artwork; icon source masks stay flat white and transparent.
    scale=2; preview=Image.new('RGBA',(512*scale,512*scale),(34,38,40,255));draw=ImageDraw.Draw(preview)
    for i in range(12):
        a=-105+i*30;b=a+30
        draw.pieslice((16*scale,16*scale,496*scale,496*scale),a,b,fill=('#C43F32' if i==0 else '#F2EEE3'))
    draw.ellipse((143*scale,143*scale,369*scale,369*scale),fill='#222628')
    for i,m in enumerate(icons):
        theta=math.radians(-90+i*30);x=256+math.cos(theta)*181;y=256+math.sin(theta)*181
        mm=m.resize((98*scale,98*scale),Image.Resampling.LANCZOS)
        tile=tint(mm,(255,255,255) if i==0 else (196,63,50));preview.alpha_composite(tile,(round((x-49)*scale),round((y-49)*scale)))
    preview=preview.resize((512,512),Image.Resampling.LANCZOS);preview.save(out/'preview.png',icc_profile=profile)
    preview.resize((128,128),Image.Resampling.LANCZOS).save(out/'preview_128.png',icc_profile=profile)
    (out/'LICENSE.txt').write_text('CC0-1.0\n\nAll twelve silhouettes, vector paths, preview layout and generator were drawn from scratch for this project. No game icons, icon fonts, stock sets, traced images or external models were used. To the extent possible under law, the authors waive copyright and related rights under CC0 1.0 Universal: https://creativecommons.org/publicdomain/zero/1.0/\n')
    (out/'check_report.json').write_text(json.dumps({'pass':True,'count':12,'pngCount':24,'svgCount':12,'singlePathPerSvg':True,'whiteStraightAlpha':True,'sRGB':True,'opaqueMarginAtLeastPercent':10,'marginAlphaThreshold':128,'icons':report},indent=2)+'\n')
    print('PASS 12 original paths /24 white-alpha PNGs, sRGB, margins and2 wheel previews')
if __name__=='__main__':main()
