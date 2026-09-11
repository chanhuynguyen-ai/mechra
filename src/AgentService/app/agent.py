"""Bounded, provider-independent v0.2 planner. It never performs CAD mutations."""
import re
import time
import unicodedata
from collections import OrderedDict
from dataclasses import dataclass
from threading import RLock

from .models import CadOperation, CadPlan, ChatRequest, ChatResponse, DesignSpec, SpecItem

NUMBER = r'[+-]?(?:\d+(?:[.,]\d+)?|[.,]\d+)'
DIMENSIONS = re.compile(rf'({NUMBER})\s*x\s*({NUMBER})(?:\s*x\s*({NUMBER}))?\s*(mm)?')
SINGLE = re.compile(rf'({NUMBER})\s*(mm)?')
# Context-only early rejection. C# still inspects actual bodies and features at Apply.
BLANK_PART_TYPES = frozenset(name.casefold() for name in (
    'HistoryFolder', 'CommentsFolder', 'FavoriteFolder', 'SelectionSetFolder', 'SensorFolder',
    'DetailCabinet', 'MaterialFolder', 'SolidBodyFolder', 'SurfaceBodyFolder', 'RefPlane', 'OriginProfileFeature',
))
HELP = "v0.2 hỗ trợ tạo plate và sửa chiều dày. Ví dụ: Tạo plate 100 x 60 x 5 mm."


def fold(text):
    text = unicodedata.normalize('NFD', text.lower()).replace('đ', 'd')
    text = ''.join(c for c in text if unicodedata.category(c) != 'Mn')
    return re.sub(r'\s+', ' ', text.replace('×', 'x').replace('*', 'x')).strip()


def number(value):
    return float(value.replace(',', '.'))


def valid(values):
    return all(0 < n <= 10000 for n in values)


@dataclass
class PendingPlate:
    width: float
    height: float
    source: str
    explicit_unit: bool
    stamp: int | None
    expires: float


class MechraAgent:
    def __init__(self, clock=time.monotonic, ttl=900, max_pending=512):
        self._pending = OrderedDict()
        self._clock, self._ttl, self._max_pending = clock, ttl, max_pending
        self._lock = RLock()

    def reply(self, request: ChatRequest) -> ChatResponse:
        with self._lock:
            now = self._clock()
            for key in list(self._pending):
                if self._pending[key].expires <= now:
                    del self._pending[key]
            return self._reply(request)

    def _reply(self, request):
        text, ctx = fold(request.message), request.context
        # Unsaved documents need an identity supplied by the add-in, not their title.
        key = (request.session_id, ctx.document_id, ctx.configuration)
        can_continue = bool(request.session_id and ctx.document_id)
        pending = self._pending.get(key) if can_continue else None
        if text in ('cancel', 'huy', 'huy bo', 'reset'):
            self._pending.pop(key, None)
            return ChatResponse(message='Đã hủy yêu cầu đang chờ.')
        blocker = self._creation_blocker(ctx)
        if pending and SINGLE.fullmatch(text) and blocker:
            self._pending.pop(key, None)
            return ChatResponse(message=blocker)
        if pending and (ctx.document_type != 'part' or self._has_plate(ctx)
                        or ctx.update_stamp != pending.stamp):
            self._pending.pop(key, None)
            pending = None
        answer = SINGLE.fullmatch(text)
        if pending and answer:
            thickness = number(answer[1])
            if not valid([thickness]):
                return ChatResponse(message='Chiều dày phải lớn hơn 0 và không vượt quá 10,000 mm.',
                                    status='clarification_required')
            self._pending.pop(key, None)
            return self._create([pending.width, pending.height, thickness], pending.source,
                                pending.explicit_unit, request.message, bool(answer[2]))
        # A new intent cannot inherit dimensions from an abandoned clarification.
        self._pending.pop(key, None)

        create = re.match(r'^(?:please |hay |vui long )?(?:tao|create|make|dung|ve|build)\b\s*', text)
        if create:
            if ctx.document_type != 'part':
                return ChatResponse(message='Hãy mở một SOLIDWORKS Part trống trước.')
            if blocker:
                return ChatResponse(message=blocker)
            rest = text[create.end():]
            if not re.search(r'\b(?:plate|tam|ban)\b', rest):
                return self._clarify(HELP)
            rest = re.sub(r'\b(?:hinh chu nhat|rectangular|plate|tam|ban|mot|a|an|new|moi)\b', ' ', rest)
            match = DIMENSIONS.fullmatch(re.sub(r'\s+', ' ', rest).strip())
            if not match:
                return self._clarify('Chưa thể diễn giải toàn bộ yêu cầu. Hãy dùng một lệnh với kích thước mm, '
                                     'ví dụ: Tạo plate 100 x 60 x 5 mm. Các lỗ/cắt/bo góc chưa thuộc v0.2.')
            values = [number(match[i]) for i in (1, 2, 3) if match[i] is not None]
            if not valid(values):
                return self._clarify('Mỗi kích thước phải lớn hơn 0 và không vượt quá 10,000 mm.')
            explicit = bool(match[4])
            if len(values) == 2:
                if can_continue:
                    self._pending[key] = PendingPlate(*values, request.message, explicit,
                                                      ctx.update_stamp, self._clock() + self._ttl)
                    while len(self._pending) > self._max_pending:
                        self._pending.popitem(last=False)
                followup = 'Có thể trả lời: 5 mm.' if can_continue else 'Hãy gửi lại lệnh đầy đủ ba kích thước.'
                return ChatResponse(message=f'Plate {values[0]:g} x {values[1]:g} mm còn thiếu chiều dày. {followup}',
                                    status='clarification_required',
                                    design_spec=self._spec(values + [None], request.message, explicit))
            return self._create(values, request.message, explicit)

        edit = re.fullmatch(rf'(?:doi|sua|change|set)\s+(?:chieu day|do day|thickness)\s*'
                            rf'(?:(?:thanh|to|la|=)\s*)?({NUMBER})\s*(mm)?', text)
        if edit:
            if ctx.document_type != 'part' or not self._has_plate(ctx):
                return ChatResponse(message='Hãy mở Part chứa Mechra-Plate-Extrude trước khi sửa chiều dày.')
            thickness = number(edit[1])
            if not valid([thickness]):
                return self._clarify('Chiều dày phải lớn hơn 0 và không vượt quá 10,000 mm.')
            op = CadOperation(id='op-1', kind='modify_plate_thickness',
                              intent='Edit the existing native blind extrusion',
                              inputs={'thickness_mm': thickness})
            spec = self._spec([None, None, thickness], request.message, bool(edit[2]))
            spec.items = [spec.items[-1]]
            return self._ready(f'Change plate thickness to {thickness:g} mm', op, spec,
                               'Chiều dài và chiều rộng sẽ được đọc từ Part và giữ nguyên.')
        if text in ('check', 'check model', 'kiem tra', 'kiem tra model', 'kiem tra mo hinh'):
            return ChatResponse(message=f"Context: {ctx.document_type}, {ctx.document_title or 'chưa có tài liệu'}, "
                                f'{len(ctx.features)} top-level features. Chẩn đoán và sửa lỗi thuộc mốc v0.3.')
        return ChatResponse(message=HELP)

    @staticmethod
    def _creation_blocker(ctx):
        if MechraAgent._has_plate(ctx):
            return ('Part đã có Mechra-Plate-Extrude. Hãy sửa chiều dày của plate hiện tại. '
                    'Muốn tạo plate khác: File > New > Part, rồi gửi lại lệnh tạo plate.')
        feature = next((f for f in ctx.features if (f.type_name or '').casefold() not in BLANK_PART_TYPES), None)
        if feature is not None:
            return (f'Part "{ctx.document_title or "hiện tại"}" có feature "{feature.name}"; '
                    'v0.2 cần Part trống để tạo plate. Chọn File > New > Part, rồi gửi lại '
                    '"Tạo plate 100 x 60 x 5 mm". Chưa tạo kế hoạch thực thi.')
        return None

    @staticmethod
    def _has_plate(ctx):
        return any(f.name.casefold() == 'mechra-plate-extrude' for f in ctx.features)

    @staticmethod
    def _clarify(message):
        return ChatResponse(message=message, status='clarification_required')

    @staticmethod
    def _spec(values, source, explicit_unit, thickness_source=None, thickness_unit=None):
        items = []
        for name, value in zip(('width_mm', 'height_mm', 'thickness_mm'), values):
            is_answer = name == 'thickness_mm' and thickness_source is not None
            unit_explicit = thickness_unit if is_answer else explicit_unit
            items.append(SpecItem(
                id=name, field=name, value=value,
                status='missing' if value is None else ('confirmed' if unit_explicit else 'assumed'),
                source=None if value is None else (thickness_source if is_answer else source),
                confidence=0 if value is None else (1 if unit_explicit else 0.8),
                required_for=['base_extrude'],
                evidence=[] if unit_explicit or value is None else ['Unit mm assumed and shown in the plan.'],
            ))
        return DesignSpec(items=items)

    def _create(self, values, source, explicit_unit, thickness_source=None, thickness_unit=None):
        width, height, thickness = values
        op = CadOperation(id='op-1', kind='create_plate',
                          intent='Create a native rectangular sketch and blind boss extrusion',
                          inputs=dict(width_mm=width, height_mm=height, thickness_mm=thickness))
        return self._ready(f'Create native plate {width:g} x {height:g} x {thickness:g} mm', op,
                           self._spec(values, source, explicit_unit, thickness_source, thickness_unit),
                           'Mặt phẳng tham chiếu đầu tiên được dùng và hiển thị trong kế hoạch.')

    @staticmethod
    def _ready(summary, operation, spec, note):
        assumed = any(i.status == 'assumed' for i in spec.items)
        return ChatResponse(message='Kế hoạch đã sẵn sàng. Xem kích thước rồi bấm Apply plan. ' + note
                            + (' Đơn vị mm được giả định vì lệnh chưa ghi đơn vị.' if assumed else ''),
                            status='plan_ready', action='execute_cad_plan', requires_confirmation=True,
                            plan=CadPlan(summary=summary, operations=[operation]), design_spec=spec)
