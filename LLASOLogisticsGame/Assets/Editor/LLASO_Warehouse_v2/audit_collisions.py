import io,contextlib,collections
with contextlib.redirect_stdout(io.StringIO()):
    import gen_warehouse as g
h=g.audit(g.m.boxes,verbose=False)
def hidden(x):
    d,v=x[4][0],x[5]
    if x[4]=='y-' and v<=0.001: return True
    if x[4]=='x-' and abs(v+12.6)<1e-6: return True
    if x[4]=='x+' and abs(v-12.6)<1e-6: return True
    if x[4]=='x-' and abs(v+13.0)<1e-6 and False: return True
    return False
h=[x for x in h if not hidden(x)]
print(len(h))
c=collections.Counter((x[0],x[1],x[2],x[3],x[4],x[5]) for x in h)
for k,v in sorted(c.items()): print(v,k)
