import bpy, math, random, os
from mathutils import Vector, noise as bn
random.seed(42)
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT=os.path.join(ROOT,'Assets','Resources','Models')
TEX=os.path.join(ROOT,'Assets','Resources','Textures')
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
def mat(name,c,metal=0,rough=.5):
 m=bpy.data.materials.new(name); m.diffuse_color=(*c,1); m.use_nodes=True
 s=m.node_tree.nodes.get('Principled BSDF'); s.inputs['Base Color'].default_value=(*c,1); s.inputs['Metallic'].default_value=metal; s.inputs['Roughness'].default_value=rough
 return m
wood=mat('Walnut',(.20,.085,.035),0,.32); brass=mat('Brass',(.58,.36,.12),.75,.26)
velvet=mat('Velvet',(.16,.025,.035),0,.85); thread=mat('Thread',(.48,.28,.10),0,.7);chaircloth=mat('ChairVelvet',(.035,.12,.10),0,.82)
felt=mat('Felt',(.035,.10,.075),0,.95); lightwood=mat('BarrelWood',(.66,.40,.18),0,.4)
def finish(o,name,m):
 o.name=name; o.data.materials.append(m)
 for p in o.data.polygons:p.use_smooth=True
 return o
def bevel(o,w=.02,seg=3):
 q=o.modifiers.new('Soft crafted edges','BEVEL');q.width=w;q.segments=seg
 o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL')
 return o
def cube(n,loc,scale,m,b=.02):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);finish(o,n,m);bevel(o,b);return o
def cyl(n,loc,r,d,m):
 bpy.ops.mesh.primitive_cylinder_add(vertices=96,radius=r,depth=d,location=loc);o=finish(bpy.context.object,n,m);bevel(o,.009);return o
def torus(n,loc,r,minor,m):
 major=192 if 'inlay' in n.lower() else 64;minorseg=6
 bpy.ops.mesh.primitive_torus_add(major_segments=major,minor_segments=minorseg,location=loc,major_radius=r,minor_radius=minor);return finish(bpy.context.object,n,m)
def curve(n,pts,r,m):
 c=bpy.data.curves.new(n,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=3;s=c.splines.new('POLY');s.points.add(len(pts)-1)
 for p,v in zip(s.points,pts):p.co=(*v,1)
 o=bpy.data.objects.new(n,c);bpy.context.collection.objects.link(o);o.data.materials.append(m);return o
def lathe(n,profile,m,segments=128):
 vs=[];fs=[]
 for r,z in profile:
  for i in range(segments):
   a=math.tau*i/segments;vs.append((r*math.cos(a),r*math.sin(a),z))
 for j in range(len(profile)-1):
  for i in range(segments):
   a=j*segments+i;b=j*segments+(i+1)%segments;fs.append((a,b,b+segments,a+segments))
 me=bpy.data.meshes.new(n);me.from_pydata(vs,[],fs);me.update();uv=me.uv_layers.new(name='Crafted UV')
 for p in me.polygons:
  for li in p.loop_indices:
   vi=me.loops[li].vertex_index;uv.data[li].uv=((vi%segments)/segments,(vi//segments)/(len(profile)-1))
 o=bpy.data.objects.new(n,me);bpy.context.collection.objects.link(o);finish(o,n,m);return o
def ellipse(n,loc,scale,m):
 detail=10 if 'tack' in n else 16 if 'cap' in n else 48
 bpy.ops.mesh.primitive_uv_sphere_add(segments=detail,ring_count=max(6,detail//2),location=loc);o=bpy.context.object;o.scale=scale;finish(o,n,m);return o
def export(name):
 objs=list(bpy.context.scene.objects);bpy.ops.object.select_all(action='SELECT')
 bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'ArtSource',name+'.blend'))
 bpy.ops.object.convert(target='MESH')
 # Merge by material for a small number of Unity draw calls. Blend sources stay fully editable.
 groups={}
 for obj in list(bpy.context.scene.objects):
  if obj.type=='MESH':groups.setdefault(obj.data.materials[0].name,[]).append(obj)
 for group in groups.values():
  bpy.ops.object.select_all(action='DESELECT')
  for obj in group:obj.select_set(True)
  bpy.context.view_layer.objects.active=group[0];bpy.ops.object.join()
 bpy.ops.object.select_all(action='SELECT')
 total=0
 for obj in bpy.context.scene.objects:
  if obj.type=='MESH':obj.data.calc_loop_triangles();total+=len(obj.data.loop_triangles)
 print('MODEL_TRIANGLES',name,total)
 bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,name+'.fbx'),use_selection=True,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
# Continuous ogee-edged walnut table and a hand-turned pedestal.
lathe('Ogee walnut edge',[(0,.77),(1.84,.77),(1.89,.78),(1.925,.80),(1.935,.82),(1.928,.84),(1.91,.85),(1.92,.87),(1.915,.89),(1.89,.90),(0,.90)],wood,192)
cyl('Deep forest baize',(0,0,.904),1.715,.008,felt)
for r in [1.741,1.764,1.886]:torus('Fine brass inlay',(0,0,.902),r,.0035,brass)
lathe('Pedestal',[(0,.07),(.28,.07),(.30,.09),(.30,.12),(.25,.14),(.21,.17),(.18,.22),(.17,.30),(.15,.40),(.18,.50),(.24,.59),(.27,.62),(.27,.65),(.22,.67),(.21,.72),(.34,.75),(0,.76)],wood)
for j in range(4):
 a=j*math.pi/2;pts=[]
 for i in range(40):
  t=i/39;r=.16+.73*t;z=.22-.15*math.sin(t*math.pi/2)+.025*math.sin(t*math.pi);pts.append((r*math.cos(a),r*math.sin(a),z))
 curve('Sweeping pedestal foot',pts,.064,wood);ellipse('Brass foot cap',(.89*math.cos(a),.89*math.sin(a),.075),(.075,.075,.045),brass)
export('Table')
# Generous open, double-sided cloth sack. Every ring deforms separately, creating hanging folds.
verts=[];faces=[];N=128;M=64
def sackpoint(j,i,inner=False):
 t=j/M;a=i/N*math.tau
 r=.28+.17*math.sin(math.pi*t)-.06*t
 # Broad asymmetric drape plus secondary folds of varying length, rather than a regular star profile.
 folds=(.020*math.sin(9*a+3*t)+.010*math.sin(16*a-5*t)+.022*math.cos(6*a+6*t))*(.2+.8*t)
 folds+=.015*math.sin(3*a+1.7)*math.sin(math.pi*t)+.008*math.sin(a*31+t*17)*math.sin(math.pi*t)**2
 folds+=.014*math.sin(t*37+2*a)*math.exp(-((t-.20)/.19)**2)
 r+=folds
 if inner:r-=.014
 z=.025+.60*t+.044*t**4*math.sin(3*a+.6)+.009*math.sin(a*9)*t*t-.025*math.sin(a*2)*math.sin(math.pi*t)
 return (r*math.cos(a)+.07*t*t,r*math.sin(a)-.025*t*t,z)
for inside in [False,True]:
 for j in range(M+1):
  for i in range(N):verts.append(sackpoint(j,i,inside))
off=(M+1)*N
for k in range(2):
 for j in range(M):
  for i in range(N):
   a=k*off+j*N+i;b=k*off+j*N+(i+1)%N;c=b+N;d=a+N
   faces.append((a,b,c,d) if k==0 else (d,c,b,a))
for i in range(N):faces.append((M*N+i,M*N+(i+1)%N,off+M*N+(i+1)%N,off+M*N+i))
mesh=bpy.data.meshes.new('Sculpted draping cloth');mesh.from_pydata(verts,[],faces);mesh.update()
uv=mesh.uv_layers.new(name='Cloth weave UV')
for poly in mesh.polygons:
 us=[(mesh.loops[li].vertex_index%N)/N for li in poly.loop_indices];seam=max(us)-min(us)>.5
 for li,u in zip(poly.loop_indices,us):
  idx=mesh.loops[li].vertex_index%off;uv.data[li].uv=(u+1 if seam and u<.5 else u,(idx//N)/M)
o=bpy.data.objects.new('Open velvet sack',mesh);bpy.context.collection.objects.link(o);finish(o,o.name,velvet)
for j in [61,63]:curve('Rolled stitched lip',[sackpoint(j,i) for i in range(N+1)],.004,velvet)
for i in range(0,N,2):
 p=Vector(sackpoint(61,i));q=Vector(sackpoint(62,i));curve('Individual golden stitch',[p,q],.0010,thread)
# A curved double seam down the front, with tiny visible lock stitches.
for offset in [-.009,.009]:
 pts=[]
 for j in range(3,61):
  p=Vector(sackpoint(j,int(N*.75)));p.x+=offset;p.y-=.003;pts.append(p)
 curve('Tailored front seam',pts,.0018,velvet)
for j in range(4,60,2):
 p=Vector(sackpoint(j,int(N*.75)));q=Vector(sackpoint(j+1,int(N*.75)));p.y-=.004;q.y-=.004;curve('Lock stitch',[p,q],.00065,thread)
for side in [-1,1]:
 pts=[]
 for i in range(100):
  t=i/99;pts.append((side*(.19+.17*t),-.22-.04*math.sin(t*math.pi),.61-.36*t+.025*math.sin(t*7)))
 curve('Braided drawstring',pts,.009,thread)
 for k in range(10):curve('Tassel',[(side*.36+(k-5)*.002,-.22,.27),(side*.37+(k-5)*.003,-.22,.20)],.002,thread)
export('Sack')
# A real lathed barrel silhouette, two recessed circular faces, grooves and end rings.
verts=[];faces=[];N=64;M=12
for j in range(M+1):
 z=-.5+j/M;r=.78+.19*math.sin(math.pi*j/M)**.65
 for i in range(N):
  a=i/N*math.tau;verts.append((r*math.cos(a),r*math.sin(a),z))
for j in range(M):
 for i in range(N):a=j*N+i;b=j*N+(i+1)%N;faces.append((a,b,b+N,a+N))
mesh=bpy.data.meshes.new('Lathed barrel');mesh.from_pydata(verts,[],faces);mesh.update()
uv=mesh.uv_layers.new(name='Turned wood UV')
for poly in mesh.polygons:
 us=[(mesh.loops[li].vertex_index%N)/N for li in poly.loop_indices];seam=max(us)-min(us)>.5
 for li,u in zip(poly.loop_indices,us):uv.data[li].uv=(u+1 if seam and u<.5 else u,(mesh.loops[li].vertex_index//N)/M)
o=bpy.data.objects.new('Barrel body',mesh);bpy.context.collection.objects.link(o);finish(o,o.name,lightwood)
for z in [-.5,.5]:
 cyl('Inset end grain',(0,0,z),.77,.025,lightwood);torus('Turned end rim',(0,0,z),.77,.035,lightwood)
for z in [-.32,.32]:torus('Dark carved groove',(0,0,z),.90,.012,wood)
export('Barrel')
# Symmetric bentwood dining chair, oval upholstered back and flowing, slightly splayed legs.
cube('Rounded seat apron',(0,0,.47),(.60,.57,.10),wood,.065)
cube('Tailored forest velvet seat',(0,-.015,.535),(.55,.51,.10),chaircloth,.055)
for x in [-1,1]:
 for y in [-1,1]:
  pts=[]
  for i in range(35):
   t=i/34;pts.append((x*(.24+.045*(1-t)**2),y*(.215+.055*(1-t)**2),.055+.42*t))
  curve('Splayed bentwood leg',pts,.025,wood)
 forback=[]
 for i in range(40):
  t=i/39;forback.append((x*(.235-.025*math.sin(t*math.pi)),.21+.07*t,.48+.59*t))
 curve('Sweeping back support',forback,.023,wood)
ellipse('Oval velvet back',(0,.284,1.015),(.238,.045,.275),chaircloth)
pts=[]
for i in range(129):
 a=i/128*math.tau;pts.append((.264*math.sin(a),.28+.018*math.cos(a),1.015+.303*math.cos(a)))
curve('Steam bent oval frame',pts,.024,wood)
curve('Upholstery piping',[(.235*math.sin(i/128*math.tau),.237,1.015+.272*math.cos(i/128*math.tau)) for i in range(129)],.003,thread)
for i in range(36):
 a=i/36*math.tau;ellipse('Inset brass tack',(.245*math.sin(a),.235,1.015+.28*math.cos(a)),(.004,.0025,.004),brass)
export('Chair')
# Deterministic original seamless textures; no external assets.
for kind in ['Walnut','BarrelWood','Velvet','Paper']:
 n=768;pix=[]
 for y in range(n):
  for x in range(n):
   u=x/n;v=y/n;noise=random.random()-.5
   if kind in ['Walnut','BarrelWood']:
    warp=bn.noise_vector(Vector((u*4,v*.8,2)))[0]
    grain=math.sin(math.tau*(u*63+warp*1.8))
    grain+=.25*math.sin(u*math.tau*260+warp*12)
    broad=bn.noise_vector(Vector((u*9,v*.4,5)))[1]
    f=.84+.07*grain+.14*broad+.045*(random.random()-.5);base=(.26,.145,.082) if kind=='Walnut' else (.62,.43,.25)
   elif kind=='Velvet':f=.88+.18*(random.random()-.5)+.04*math.sin(x*math.pi/2)*math.cos(y*math.pi/2);base=(.27,.045,.065)
   else:f=.95+.06*(random.random()-.5)-.012*math.sin(u*41)*math.sin(v*31);base=(.82,.79,.71)
   pix.extend([min(1,c*f) for c in base]+[1])
 im=bpy.data.images.new(kind,width=n,height=n);im.pixels=pix;im.filepath_raw=os.path.join(TEX,kind+'.png');im.file_format='PNG';im.save()
print('MODEL_PIPELINE_COMPLETE')
