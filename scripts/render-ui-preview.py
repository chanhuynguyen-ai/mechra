"""Illustrative SVG layout, not a capture of the native WinForms renderer.
Requires Pillow only to measure text. Optional PNG conversion: Inkscape/Sharp.
"""
from pathlib import Path
from html import escape
from PIL import ImageFont
ROOT = Path(__file__).resolve().parents[1]
C = dict(bg='#090a0c', surface='#111317', raised='#191c21', border='#2b2f37', text='#edf0f4', muted='#97a0af', accent='#a6cbdc', green='#73d8ae')
svg=['<svg xmlns="http://www.w3.org/2000/svg" width="1266" height="1110" viewBox="0 0 1266 1110">']
def rect(x,y,w,h,fill,r=0,stroke='none'):
    svg.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}" stroke="{stroke}"/>')
def txt(x,y,s,size=13,color='text',bold=False):
    svg.append(f'<text x="{x}" y="{y}" fill="{C.get(color,color)}" font-family="DejaVu Sans, sans-serif" font-size="{size}" font-weight="{700 if bold else 400}">{escape(str(s))}</text>')
def para(x,y,s,width,size=13,color='muted',leading=21):
    font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',size)
    words=s.split();line=''
    for word in words:
        test=(line+' '+word).strip()
        if font.getlength(test)>width and line:
            txt(x,y,line,size,color);y+=leading;line=word
        else:line=test
    if line:txt(x,y,line,size,color);y+=leading
    return y
def button(x,y,w,s,primary=False):
    rect(x,y,w,34,C['text'] if primary else C['raised'],8,C['border'])
    font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',12)
    txt(x+(w-font.getlength(s))/2,y+22,s,12,'bg' if primary else 'text')
def wire(x,y,s=13):
    pts=[(x,y-s),(x+s,y-s/2),(x+s,y+s/2),(x,y+s),(x-s,y+s/2),(x-s,y-s/2)]
    svg.append('<path d="M '+' L '.join(f'{a},{b}' for a,b in pts)+f' Z M {x-s},{y-s/2} L {x},{y} L {x+s},{y-s/2} M {x},{y} L {x},{y+s}" stroke="{C["accent"]}" fill="none" stroke-width="1.5"/>')
def card(x,y,h,heading,color='accent'):
    rect(x+14,y,362,h,C['surface'],10,C['border']);txt(x+29,y+27,heading,10,color,True)
def metrics(x,y,values):
    rect(x,y,332,72,C['raised'],5)
    for i,(k,v) in enumerate(zip(['RỘNG · mm','CAO · mm','DÀY · mm'],values)):
        txt(x+12+i*108,y+23,k,10,'muted');txt(x+12+i*108,y+51,v,20,'text',True)
def shell(x,index,label):
    txt(x,100,f'0{index}  /  {label}',11,'muted',True)
    rect(x,120,390,946,C['bg'],14,C['border'])
    wire(x+33,155);txt(x+58,163,'Mechra',23,'text',True);button(x+341,138,33,'+')
    txt(x+58,186,'NATIVE CAD   /   LOCAL PLANNER',9,'muted')
    txt(x+15,217,'Chưa kiểm tra Part' if index==1 else 'Part2  /  Default',12,'muted')
    # Fixed composer and secondary tools. Conversation scrolls above this area.
    button(x+14,887,102,'Check Part');button(x+125,887,88,'Save log')
    rect(x+14,931,362,95,C['surface'],10,C['border'])
    txt(x+28,956,'Mô tả plate bạn muốn tạo...',13,'muted')
    txt(x+28,1009,'Shift + Enter xuống dòng',10,'muted');button(x+296,982,65,'Gửi ↑',True)
    txt(x+14,1050, 'Sẵn sàng  ·  0.2.0-dev.5' if index!=3 else 'Đã kiểm chứng · Part native có thể chỉnh sửa',10,'muted')
rect(0,0,1266,1110,'#070809')
txt(24,38,'Mechra',26,'text',True);txt(155,37,'NATIVE CAD WORKSPACE',11,'accent',True)
txt(24,65,'Bản xem trước bố cục dev.5 · Dữ liệu minh họa · Chưa phải ảnh chạy WinForms trong SOLIDWORKS',12,'muted')
for i,(x,label) in enumerate([(24,'BẮT ĐẦU'),(438,'XEM KẾ HOẠCH'),(852,'KẾT QUẢ')],1):shell(x,i,label)
x=24;card(x,242,467,'THIẾT KẾ CÙNG MECHRA');wire(x+54,330,26)
txt(x+29,396,'Từ ý tưởng',27,'text',True);txt(x+29,433,'đến Part native.',27,'text',True)
para(x+29,469,'Tạo plate có kích thước điều khiển, sửa chiều dày và kiểm chứng sau rebuild.',322,13)
txt(x+29,544,'01 Mô tả  →  02 Xem kế hoạch  →  03 Áp dụng',10,'accent')
button(x+29,562,332,'Tạo plate 100 × 60 × 5 mm');button(x+29,606,332,'Đổi chiều dày thành 8 mm')
para(x+29,665,'Bắt đầu với Part trống. Bộ lập kế hoạch cục bộ; chưa kết nối mô hình AI.',320,11,leading=18)
x=438;card(x,242,91,'BẠN','muted');txt(x+29,302,'Tạo plate 100 x 60 x 5 mm',14)
card(x,347,429,'CAD PLAN  /  01 OPERATION')
txt(x+29,409,'Tạo plate native',19,'text',True);txt(x+29,436,'Part2  ·  Default',12,'muted');metrics(x+29,456,['100','60','5'])
y=para(x+29,554,'Sketch chữ nhật trên mặt phẳng tham chiếu đầu tiên. Extrude theo chiều dày đã chọn.',329,12)
y=para(x+29,y+8,'Đơn vị: mm. Giá trị không ghi đơn vị được hiểu là mm.',329,11,leading=18)
para(x+29,y+8,'Tự động rebuild và đối chiếu kích thước, thể tích sau khi thực thi.',329,12)
txt(x+29,710,'CHỜ BẠN XÁC NHẬN',10,'accent',True);button(x+29,726,165,'Áp dụng kế hoạch',True);button(x+204,726,61,'Hủy')
x=852;card(x,242,91,'BẠN','muted');txt(x+29,302,'Đổi chiều dày thành 8 mm',14)
card(x,347,165,'CAD PLAN  /  01 OPERATION');txt(x+29,408,'Cập nhật chiều dày',19,'text',True)
txt(x+29,442,'Chiều dày mới: 8 mm',13);txt(x+29,482,'Đã gửi thực thi',11,'muted')
card(x,526,319,'VERIFIED  /  ĐÃ KIỂM CHỨNG','green');txt(x+29,586,'Part đã sẵn sàng.',19,'text',True)
txt(x+29,614,'KÍCH THƯỚC ĐO LẠI',10,'muted');metrics(x+29,629,['100','60','8'])
txt(x+29,727,'Thể tích: 48000 mm³',13)
para(x+29,754,'Rebuild thành công. Kích thước và thể tích nằm trong dung sai kiểm chứng.',328,12,leading=19)
button(x+29,800,176,'+ Chi tiết kỹ thuật')
txt(24,1090,'Black foundation · Native controls · Review before apply · Measured results',11,'muted')
svg.append('</svg>')
(ROOT/'docs/ui/mechra-dev5-preview.svg').write_text('\n'.join(svg),encoding='utf-8')
