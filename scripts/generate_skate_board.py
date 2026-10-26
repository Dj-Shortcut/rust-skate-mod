#!/usr/bin/env python3
"""Original CC0 skateboard rigid rig. Blender 5.x, no external assets.
blender -b --factory-startup --python generate_skate_board.py -- --output DIR
Conceptual coordinates are glTF meters; B(x,y,z)=(G.x,-G.z,G.y).
Only bushings carry a fixed rest rotation: runtime scales them, never rotates them.
"""
import argparse, json, math, os, struct, sys, hashlib
from pathlib import Path
import bpy, bmesh
from mathutils import Vector, Quaternion, Matrix
from mathutils.bvhtree import BVHTree

parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True)
parser.add_argument('--no-previews', action='store_true', help='Export and validate the rig without render-only studio previews.')
args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
OUT = Path(args.output).resolve(); OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
for m in list(bpy.data.materials): bpy.data.materials.remove(m)
scene = bpy.context.scene
scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1.0
asset=[]; origins={}; axes={}; fixed=[]; moving={}; required=[]

def B(g): return Vector((g[0],-g[2],g[1]))
def G(b): return [round(b[0],9),round(b[2],9),round(-b[1],9)]
def mat(name, color, metal=0, rough=.5):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Metallic'].default_value=metal; bs.inputs['Roughness'].default_value=rough
    return m
wood=mat('Maple natural',(.55,.29,.105),0,.48)
plylight=mat('Maple pale ply',(.72,.49,.25),0,.53)
plydark=mat('Maple dark glue ply',(.22,.095,.036),0,.55)
grip=mat('Black griptape',(.018,.022,.027),0,.92)
aluminium=mat('Brushed cast aluminum',(.57,.64,.69),.83,.27)
steel=mat('Black oxide steel',(.075,.095,.11),.85,.3)
urethane=mat('Warm off white urethane',(.86,.82,.67),0,.47)
bushingmat=mat('Amber urethane bushings',(.95,.29,.028),0,.38)
cyan=mat('Original turquoise graphic',(.025,.58,.55),0,.42)
red=mat('Original vermilion graphic',(.94,.13,.055),0,.42)
bearingmat=mat('Bearing shields',(.20,.24,.29),.9,.21)

class Geo:
    def __init__(self): self.v=[]; self.f=[]
    def add(self, verts, faces):
        off=len(self.v); self.v.extend([Vector(p) for p in verts]); self.f.extend([tuple(off+i for i in f) for f in faces])
    def box(self,c,s):
        x,y,z=c; a,b,d=[v/2 for v in s]
        self.add([(x+i*a,y+j*b,z+k*d) for i,j,k in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]],
            [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])
    def tube(self,center,axis,length,radius,segments=12,inner=0):
        c=Vector(center); a=Vector(axis).normalized(); u=a.cross(Vector((0,1,0)))
        if u.length<.1: u=a.cross(Vector((1,0,0)))
        u.normalize(); w=a.cross(u).normalized()
        profile=[(-length/2,radius),(length/2,radius)]
        if inner: profile.extend([(length/2,inner),(-length/2,inner)])
        else: profile.extend([(length/2,0),(-length/2,0)])
        self.lathe(c,a,u,w,profile,segments)
    def lathe(self,c,a,u,w,profile,segments=24):
        verts=[]
        for along,r in profile:
            for i in range(segments):
                angle=2*math.pi*i/segments
                verts.append(c+a*along+r*(u*math.cos(angle)+w*math.sin(angle)))
        faces=[]
        for j in range(len(profile)):
            k=(j+1)%len(profile)
            for i in range(segments):
                q=(i+1)%segments; faces.append((j*segments+i,j*segments+q,k*segments+q,k*segments+i))
        self.add(verts,faces)
    def rod(self,a,b,r,segments=10):
        a,b=Vector(a),Vector(b); self.tube((a+b)*.5,b-a,(b-a).length,r,segments)

def obj(name,geo,material,parent,origin,rotation=None,smooth=False):
    origin=Vector(origin); q=rotation or Quaternion()
    mesh=bpy.data.meshes.new(name+'_mesh')
    # Input points are in conceptual glTF world coordinates. Convert into each node's local frame.
    verts=[q.inverted() @ (B(p)-B(origin)) for p in geo.v]
    mesh.from_pydata(verts,[],geo.f); mesh.materials.append(material); mesh.update()
    bm=bmesh.new(); bm.from_mesh(mesh); bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=1e-8)
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
    bmesh.ops.triangulate(bm,faces=bm.faces); bm.to_mesh(mesh); bm.free(); mesh.update()
    for p in mesh.polygons: p.use_smooth=smooth
    o=bpy.data.objects.new(name,mesh); scene.collection.objects.link(o)
    if parent: o.parent=parent; o.location=parent.matrix_world.inverted() @ B(origin)
    else: o.location=B(origin)
    o.rotation_mode='QUATERNION'; o.rotation_quaternion=q
    o['role']=name; asset.append(o); origins[name]=list(origin); axes[name]=[0,0,0]
    bpy.context.view_layer.update()
    return o

root=bpy.data.objects.new('skate_board',None); scene.collection.objects.link(root); root['role']='skate_board'; asset.append(root); origins[root.name]=[0,0,0]; axes[root.name]=[0,0,0]

def halfwidth(z):
    a=abs(z)
    return .102 if a<=.285 else .102*math.sqrt(max(0,1-((a-.285)/.115)**2))
def top(x,z): return .100 + .0055*(x/.102)**2 + math.tan(math.radians(17))*max(0,abs(z)-.285)
# Popsicle outline: rounded nose/tail, seven thin visible ply layers, concave and 17-degree kicks.
zs=[-.4,-.392,-.375,-.35,-.32,-.285,-.20,-.10,0,.10,.20,.285,.32,.35,.375,.392,.4]
xs=[-1,-.66,-.33,0,.33,.66,1]
def deck_surface(geo,upper=True,inset=1,offset=0):
    verts=[]
    for z0 in zs:
        z=z0*inset
        for f in xs:
            x=halfwidth(z0)*f*inset; verts.append((x,top(x,z)- (0 if upper else .008)+offset,z))
    faces=[]; n=len(xs)
    for j in range(len(zs)-1):
        for i in range(n-1):
            a=j*n+i; face=(a,a+1,a+n+1,a+n); faces.append(face if upper else face[::-1])
    geo.add(verts,faces)
outline=[(-halfwidth(z),z) for z in zs]+[(halfwidth(z),z) for z in reversed(zs)]
def sideband(geo,lo,hi):
    v=[]
    for x,z in outline: v.extend([(x,top(x,z)-.008+lo,z),(x,top(x,z)-.008+hi,z)])
    f=[]
    for i in range(len(outline)):
        a=i*2; b=((i+1)%len(outline))*2; f.append((a,b,b+1,a+1))
    geo.add(v,f)
g=Geo(); deck_surface(g,True); deck_surface(g,False)
deck=obj('deck',g,wood,root,(0,.092,0),smooth=True); fixed.append(deck)
for i in range(7):
    g=Geo(); sideband(g,i*.008/7,(i+1)*.008/7)
    o=obj('deck_ply_'+str(i+1),g,plydark if i%2 else plylight,deck,(0,.092,0),smooth=True); fixed.append(o)
g=Geo(); deck_surface(g,True,.978,.00035)
o=obj('deck_grip',g,grip,deck,(0,.092,0),smooth=True); fixed.append(o)
# Authored geometric underside graphic, original construction rather than image textures.
def graphic(name,points,material):
    g=Geo(); v=[(x,top(x,z)-.008-.00035,z) for x,z in points]; g.add(v,[tuple(range(len(v)))])
    o=obj(name,g,material,deck,(0,.092,0)); fixed.append(o)
graphic('deck_graphic_turquoise',[(-.084,-.21),(.075,-.15),(.087,.205),(-.066,.15)],cyan)
graphic('deck_graphic_lightning',[(-.027,-.17),(.049,-.03),(.008,-.025),(.057,.15),(-.057,-.012),(-.012,-.012)],red)
for i,z in enumerate([-.11,-.07,.085,.125]):
    graphic('deck_graphic_slash_'+str(i),[(-.073,z),(-.031,z+.012),(-.033,z+.024),(-.075,z+.012)],plylight)

for side,sign in [('front',1),('rear',-1)]:
    z=sign*.245; pivot=Vector((0,.059,z)); axis=Vector((0,math.sqrt(.5),-sign*math.sqrt(.5)))
    # Fixed aluminum plate, four bolt seats and a cup cast into its underside.
    g=Geo(); g.box((0,.083,z),(.061,.008,.065))
    g.tube((0,.074,z+sign*.028),(0,1,0),.012,.007,12,inner=.003)
    # Steel kingpin passes through the open hanger eye; it never touches the eye wall.
    g.tube(pivot+axis*.001,axis,.046,.0025,12)
    for off in [-.020,.022]: g.tube(pivot+axis*off,axis,.002,.007,12,inner=.0026)
    base=obj('truck_'+side+'_base',g,aluminium,deck,(0,.083,z)); fixed.append(base)
    g=Geo()
    for x in [-.021,.021]:
        for zz in [-.022,.022]:
            g.tube((x,top(x,z+zz)+.001,z+zz),(0,1,0),.002,.0032,8)
            # Bolt stems embedded in baseplate; deck coupling is intentional, not moving collision.
    o=obj('truck_'+side+'_bolts',g,steel,base,(0,.083,z)); fixed.append(o)
    g=Geo(); g.tube(pivot+axis*.024,axis,.004,.0046,6,inner=.0026)
    o=obj('truck_'+side+'_kingpin_nut',g,steel,base,pivot+axis*.024); fixed.append(o)
    # The eye, yoke, axle and nuts share one rigid one-material mesh.
    g=Geo(); g.tube(pivot,axis,.005,.0115,16,inner=.0065)
    g.rod((-.052,.035,z),(.052,.035,z),.006,12)
    g.rod((-.012,.051,z),(-.044,.035,z),.0048,10)
    g.rod((.012,.051,z),(.044,.035,z),.0048,10)
    g.tube((0,.027,z),(1,0,0),.205,.003,12)
    for x in [-.102,.102]: g.tube((x,.027,z),(1,0,0),.004,.0048,6,inner=.0031)
    hanger=obj('truck_'+side+'_hanger',g,aluminium,deck,pivot)
    hanger['axis']=list(axis); axes[hanger.name]=list(axis); required.append(hanger)
    moving[side]={'hanger':hanger,'wheels':[],'axis':B(axis),'base':base}
    for end,x in [('left',-.082),('right',.082)]:
        center=Vector((x,.027,z)); g=Geo()
        profile=[(-.016,.006),(-.016,.018),(-.0145,.023),(-.011,.026),(-.006,.027),(.006,.027),(.011,.026),(.0145,.023),(.016,.018),(.016,.006)]
        g.lathe(center,Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)),profile,24)
        wheel=obj('wheel_'+side+'_'+end,g,urethane,hanger,center,smooth=True)
        wheel['axis']=[1.,0.,0.]; axes[wheel.name]=[1,0,0]; required.append(wheel)
        wheelgroup=[wheel]
        g=Geo()
        for off in [-.0161,.0161]: g.tube(center+Vector((off,0,0)),(1,0,0),.0008,.007,16,inner=.0032)
        bearing=obj('bearing_'+side+'_'+end,g,bearingmat,wheel,center,smooth=True); wheelgroup.append(bearing)
        # An asymmetric small colored sector on each side makes actual X-axis spin visible.
        g=Geo()
        for off in [-.0166,.0166]:
            v=[]
            for r in [.011,.016]:
                for j in range(5):
                    a=math.radians(25+j*8); v.append(center+Vector((off,r*math.cos(a),r*math.sin(a))))
            g.add(v,[(i,i+1,6+i,5+i) for i in range(4)])
        logo=obj('wheel_mark_'+side+'_'+end,g,red,wheel,center); wheelgroup.append(logo)
        moving[side]['wheels'].append(wheelgroup)
    # Exception explicitly requested: fixed local-Y alignment; scale-only runtime bushings.
    q=Vector((0,1,0)).rotation_difference(axis)
    # Convert the conceptual rotation into Blender's basis.
    basis=Matrix(((1,0,0),(0,0,-1),(0,1,0)))
    qb=(basis @ q.to_matrix() @ basis.transposed()).to_quaternion()
    for name,seat,tip in [('top',.019,.0045),('bottom',-.018,-.0045)]:
        origin=pivot+axis*seat; g=Geo(); c=(seat+tip)*.5; h=abs(tip-seat)
        # Slight conical barrel profile, with a true kingpin bore.
        profile=[(-h/2,.0030),(-h/2,.0082),(-h*.3,.0093),(h*.3,.0093),(h/2,.0082),(h/2,.0030)]
        g.lathe(pivot+axis*c,axis,Vector((1,0,0)),axis.cross(Vector((1,0,0))),profile,16)
        bush=obj('bushing_'+side+'_'+name,g,bushingmat,deck,origin,rotation=qb,smooth=True)
        bush['axis']=[0.,1.,0.]; bush['fixed_rest_rotation']=True; bush['runtime']='scale_only'
        axes[bush.name]=[0,1,0]; required.append(bush)

# The exporter must see only original rig objects, with one material and triangulated data each.
bpy.context.view_layer.update()
for o in asset:
    if o.type=='MESH':
        assert len(o.data.materials)==1
        assert all(len(f.vertices)==3 for f in o.data.polygons)
triangles=sum(len(o.data.polygons) for o in asset if o.type=='MESH')
assert triangles<8000, triangles

# Actual transformed triangle BVHs at 0.5-degree intervals, both signs, both trucks.
def tree(objects):
    verts=[]; faces=[]
    for o in objects:
        off=len(verts); verts.extend([o.matrix_world @ v.co for v in o.data.vertices])
        faces.extend([tuple(off+i for i in f.vertices) for f in o.data.polygons])
    return BVHTree.FromPolygons(verts,faces,all_triangles=True,epsilon=0),verts

def overlap_report():
    failures=[]; sample_count=0; minimum=1e9; nearest=None
    # Deck plus all fixed hardware. Bushings are compliant intentional contact parts, excluded.
    fixedtree,_=tree(fixed)
    for i in range(81):
        angle=-20+i*.5
        for side,sign in [('front',1),('rear',-1)]:
            data=moving[side]; data['hanger'].rotation_quaternion=Quaternion(data['axis'],math.radians(angle*sign))
        bpy.context.view_layer.update()
        for side in ['front','rear']:
            data=moving[side]
            for label,objects in [('hanger',[data['hanger']])]+[('wheel_'+str(j),group) for j,group in enumerate(data['wheels'])]:
                bvh,verts=tree(objects); contacts=bvh.overlap(fixedtree); sample_count+=1
                if contacts: failures.append({'angle_degrees':angle,'part':side+'_'+label,'triangle_pairs':len(contacts)})
                for v in verts:
                    n=fixedtree.find_nearest(v)
                    if n and n[3]<minimum: minimum=n[3]; nearest={'angle_degrees':angle,'part':side+'_'+label}
    for data in moving.values(): data['hanger'].rotation_quaternion=Quaternion()
    bpy.context.view_layer.update()
    result={'pass':not failures,'method':'Actual world-space triangulated BVHTree overlap at every 0.5 degree from -20 to +20; each wheel incl bearing/mark and hanger vs all fixed deck/base/kingpin/bolts. Compliant bushings excluded from hard-contact test.', 'samples':sample_count,'angles_per_truck':81,'minimum_sampled_vertex_surface_gap_m':minimum,'minimum_at':nearest,'failures':failures}
    (OUT/'clearance_check.json').write_text(json.dumps(result,indent=2)+'\n')
    print('SKATE_CLEARANCE', 'PASS' if result['pass'] else 'FAIL',sample_count,'pairs; min vertex gap',minimum,flush=True)
    assert result['pass'], json.dumps(failures[:8])
    return result
clearance=overlap_report()

bpy.ops.object.select_all(action='DESELECT')
for o in asset: o.select_set(True)
bpy.context.view_layer.objects.active=deck
bpy.ops.export_scene.gltf(filepath=str(OUT/'skate_board.glb'),export_format='GLB',use_selection=True,
    export_extras=True,export_apply=True,export_animations=False,export_skins=False,export_morph=False,
    export_cameras=False,export_lights=False,export_yup=True,export_materials='EXPORT',export_normals=True,
    export_texcoords=False,export_attributes=False)

# Independently read the binary header, JSON and actual indexed primitive count after export.
raw=(OUT/'skate_board.glb').read_bytes(); magic,version,total=struct.unpack_from('<III',raw)
assert magic==int.from_bytes(b'glTF','little') and version==2 and total==len(raw)
size,kind=struct.unpack_from('<II',raw,12); assert kind==int.from_bytes(b'JSON','little')
j=json.loads(raw[20:20+size]); bins=[]; offset=12
while offset<len(raw):
    ln,ty=struct.unpack_from('<II',raw,offset); bins.append((ty,ln)); offset+=8+ln
assert len(j.get('buffers',[]))==1 and 'uri' not in j['buffers'][0]
assert [ty for ty,ln in bins]==[int.from_bytes(b'JSON','little'),int.from_bytes(b'BIN\0','little')]
assert not any(j.get(k) for k in ['skins','animations','cameras','images','textures'])
parents={c:n for n,node in enumerate(j['nodes']) for c in node.get('children',[])}
name_index={n['name']:i for i,n in enumerate(j['nodes'])}
world={}
def wm(i):
    if i in world:return world[i]
    n=j['nodes'][i]; t=Vector(n.get('translation',[0,0,0])); rq=n.get('rotation',[0,0,0,1]); q=Quaternion((rq[3],rq[0],rq[1],rq[2])); s=Vector(n.get('scale',[1,1,1]))
    m=Matrix.Translation(t) @ q.to_matrix().to_4x4() @ Matrix.Diagonal((*s,1))
    world[i]=wm(parents[i])@m if i in parents else m; return world[i]
nodes=[]
for i,n in enumerate(j['nodes']):
    assert 'matrix' not in n and n['extras']['role']==n['name']
    assert n.get('scale',[1,1,1])==[1,1,1]
    bush=n['name'].startswith('bushing_')
    if not bush: assert sum(abs(a-b) for a,b in zip(n.get('rotation',[0,0,0,1]),[0,0,0,1]))<1e-6,(n['name'],n.get('rotation'))
    parent=j['nodes'][parents[i]]['name'] if i in parents else None
    expected=origins[n['name']]; actual=list(wm(i).translation)
    assert (Vector(expected)-Vector(actual)).length<1e-6,(n['name'],expected,actual)
    axis=n.get('extras',{}).get('axis',[0,0,0]); axis_world=wm(i).to_3x3()@Vector(axis)
    if bush:
        sign=1 if '_front_' in n['name'] else -1
        assert (axis_world-Vector((0,math.sqrt(.5),-sign*math.sqrt(.5)))).length<1e-6
    limit={'degrees':[-20,20]} if n['name'].endswith('_hanger') else {'degrees':'unlimited'} if n['name'].startswith('wheel_') and not n['name'].startswith('wheel_mark_') else {'compression':[0,.35],'bulge':'0.4 * compression','runtime_rotation':'never; preserve fixed rest quaternion'} if bush else {'motion':'fixed to parent'}
    nodes.append({'name':n['name'],'parent':parent,'origin_gltf_m':[round(v,9) for v in actual],'translation_local_m':n.get('translation',[0,0,0]),'rest_rotation_xyzw':n.get('rotation',[0,0,0,1]),'axis_local':axis,'axis_world_rest':[round(v,9) for v in axis_world],'allowed_range':limit})
exportedtris=0
for mesh in j['meshes']:
    assert len(mesh['primitives'])==1
    p=mesh['primitives'][0]; assert p.get('mode',4)==4 and all(k in p['attributes'] for k in ['POSITION','NORMAL'])
    exportedtris+=j['accessors'][p['indices']]['count']//3
assert exportedtris==triangles and exportedtris<8000
for n in required:assert n.name in name_index
rig={'license':'CC0-1.0','asset':'skate_board.glb','units':'meters','axes':{'up':'+Y','nose':'+Z','right':'+X'},'root_origin':'ground, centered below deck','deck_length_m':.8,'deck_width_m':.204,'deck_top_center_m':.1,'truck_z_m':[-.245,.245],'wheel_radius_m':.027,'wheel_width_m':.032,'wheel_centres_abs_x_m':.082,'wheel_centres_y_m':.027,'triangles':exportedtris,'nodes':nodes,'bushing_exception':'Only bushings preserve a fixed quaternion aligning local +Y to the kingpin. Runtime scales bushings only; never overwrite their rotation. All other nodes have identity rest rotations and unit scales.','runtime_steering':'Set front hanger angle +0.8*L degrees about axis_local and rear -0.8*L; whole-board roll -L about +Z. Wheels spin about local +X.','clearance_check':clearance,'integration_boundary':'Standalone authored rig only. Current procedural runtime axle constant is 0.23m; this asset follows requested +/-0.245m and is not installed in the mod.'}
(OUT/'skate_board_rig.json').write_text(json.dumps(rig,indent=2)+'\n')
(OUT/'gltf_readback.json').write_text(json.dumps({'pass':True,'sha256':hashlib.sha256(raw).hexdigest(),'bytes':len(raw),'triangles':exportedtris,'nodes':len(nodes),'meshes':len(j['meshes']),'binary_chunks':bins,'checks':['one embedded binary buffer','one triangle primitive/material per mesh','POSITION and NORMAL','TRS only','identity non-bushing rotations','unit node scales','all extras.role exact','world pivots and nose axis','fixed local Y bushing kingpin alignment','no skins morph animation camera light textures']},indent=2)+'\n')
# Register only this asset, preserving all other authored entries and their order.
# Use exported world vertices: accessor bounds alone are local to their rig nodes.
json_end=20+size
binary_size,binary_kind=struct.unpack_from('<II',raw,json_end)
assert binary_kind==int.from_bytes(b'BIN\0','little')
binary=raw[json_end+8:json_end+8+binary_size]
scene_index=j.get('scene',0); exported_scene=j['scenes'][scene_index]
world_points=[]; exported_vertices=0
pending=list(exported_scene.get('nodes',[])); scene_nodes=set()
while pending:
    node_index=pending.pop()
    assert node_index not in scene_nodes, 'Repeated node in exported scene'
    scene_nodes.add(node_index); node=j['nodes'][node_index]
    pending.extend(node.get('children',[]))
    if 'mesh' not in node: continue
    for primitive in j['meshes'][node['mesh']]['primitives']:
        accessor=j['accessors'][primitive['attributes']['POSITION']]
        assert accessor['type']=='VEC3' and accessor['componentType']==5126 and 'sparse' not in accessor
        view=j['bufferViews'][accessor['bufferView']]
        begin=view.get('byteOffset',0)+accessor.get('byteOffset',0)
        stride=view.get('byteStride',12); exported_vertices+=accessor['count']
        for vertex_index in range(accessor['count']):
            position=Vector(struct.unpack_from('<3f',binary,begin+vertex_index*stride))
            world_points.append(wm(node_index) @ position)
assert world_points, 'Exported scene has no mesh vertices'
bounds=[[min(point[axis] for point in world_points) for axis in range(3)],
        [max(point[axis] for point in world_points) for axis in range(3)]]
manifest_path=OUT/'manifest.json'
manifest=json.loads(manifest_path.read_text()) if manifest_path.is_file() else {
    'schema':1,'license':'CC0-1.0','units':'meters','source':'scripts/generate_skate_board.py',
    'generator_blender':bpy.app.version_string,'assets':[]}
if not isinstance(manifest.get('assets'),list): raise ValueError('Existing asset manifest has no assets list')
entry={'name':'skate_board','file':'skate_board.glb','vertices':exported_vertices,'triangles':exportedtris,
    'bounds_space':'glTF Y-up, forward +Z (standalone rigid board)','animations':[],
    'license':'CC0-1.0','source':'scripts/generate_skate_board.py','generator_blender':bpy.app.version_string,
    'default_scene':exported_scene.get('name','Scene'),
    'dimensions':{'kind':'skateboard_rig','pivot':'ground centre','length':.8,'width':.204,
        'deck_top':.1,'axle_half_span':.245,'wheel_radius':.027},
    'sha256':hashlib.sha256(raw).hexdigest(),'bytes':len(raw),'bounds':bounds}
entries=[]; replaced=False
for previous in manifest['assets']:
    if previous.get('name')=='skate_board':
        if not replaced: entries.append(entry); replaced=True
    else: entries.append(previous)
if not replaced: entries.append(entry)
manifest['assets']=entries
manifest_path.write_text(json.dumps(manifest,indent=2)+'\n')

# Save editable rest source before adding render-only studio objects.
scene['original_work_license']='CC0-1.0'; scene['units_axes']='meters; Blender x,-z,y from glTF x,y,z'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'skate_board.blend'))
if args.no_previews:
    print('SKATE_RIG PASS',exportedtris,'triangles',len(nodes),'nodes',len(raw),'bytes; previews skipped',flush=True)
    raise SystemExit(0)

# Render-only studio; none of these objects is in the exported GLB or saved editable asset.
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1400; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.film_transparent=False
scene.world.color=(.12,.12,.12)
scene.view_settings.view_transform='AgX'
# Large plane is below wheels in the rest view, no hidden asset geometry.
plane=Geo(); plane.box((0,-.003,0),(5,.004,5))
backdrop=mat('Studio only charcoal',(.035,.046,.059),0,.7)
studio=obj('__render_floor',plane,backdrop,None,(0,-.003,0)); asset.remove(studio)
def light(name,position,energy,size):
    data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.shape='DISK'; data.size=size
    ob=bpy.data.objects.new(name,data); scene.collection.objects.link(ob); ob.location=B(position)
    ob.rotation_euler=(B((0,.05,0))-ob.location).to_track_quat('-Z','Y').to_euler()
light('Studio key',(.5,1.3,.7),85,1.1); light('Studio rim',(-.7,.7,-.8),70,.8); light('Studio fill',(.6,.35,-.6),35,.7)
camera_data=bpy.data.cameras.new('Studio camera'); camera=bpy.data.objects.new('Studio camera',camera_data);scene.collection.objects.link(camera);scene.camera=camera
camera_data.type='ORTHO'
def render(name,position,target,scale):
    camera.location=B(position); camera.rotation_euler=(B(target)-camera.location).to_track_quat('-Z','Y').to_euler(); camera_data.ortho_scale=scale
    scene.render.filepath=str(OUT/name); bpy.ops.render.render(write_still=True)
render('preview_rest.png',(.85,.54,1.05),(0,.06,0),1.05)
# Left lean is +25-degree board roll, front/rear opposed 20-degree steering. Lift only studio pose.
root.rotation_mode='QUATERNION'; root.rotation_quaternion=Quaternion(B((0,0,1)),math.radians(25)); root.location.z=0
for side,sign in [('front',1),('rear',-1)]:
    moving[side]['hanger'].rotation_quaternion=Quaternion(moving[side]['axis'],math.radians(-20*sign))
for o in asset:
    if o.name.startswith('bushing_'):
        c=.28 if o.name.endswith('_top') else .06; o.scale=(1+c*.4,1+c*.4,1-c)  # glTF local Y is Blender local Z
bpy.context.view_layer.update()
root.location.z=-min((o.matrix_world@v.co).z for o in asset if o.type=='MESH' for v in o.data.vertices)
bpy.context.view_layer.update(); render('preview_left_lean.png',(.85,.45,1.05),(0,.1,0),1.05)
root.rotation_quaternion=Quaternion();root.location=Vector((0,0,0))
for d in moving.values():d['hanger'].rotation_quaternion=Quaternion()
for o in asset:o.scale=(1,1,1)
bpy.context.view_layer.update()
# Render an explicit local kingpin-axis line and arrow; not part of the rig.
pivot=Vector((0,.059,.245));axis=Vector((0,math.sqrt(.5),-math.sqrt(.5)))
guide=pivot+Vector((.022,0,0))
g=Geo();g.rod(guide-axis*.037,guide+axis*.050,.00065,8)
g.tube(guide+axis*.047,axis,.008,.0032,10)
line=obj('__render_axis',g,red,None,pivot); asset.remove(line)
render('preview_truck_axis.png',(.23,.095,.43),(0,.055,.245),.25)
print('SKATE_RIG PASS',exportedtris,'triangles',len(nodes),'nodes',len(raw),'bytes',flush=True)
