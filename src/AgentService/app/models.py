from typing import Annotated, Any, Literal
from pydantic import BaseModel, ConfigDict, Field, StrictBool, field_validator, model_validator

Dimension = Annotated[float, Field(gt=0, le=10000, allow_inf_nan=False, strict=True)]
Positive = Annotated[float, Field(gt=0, allow_inf_nan=False, strict=True)]
OperationKind = Literal['create_plate', 'modify_plate_thickness']


class Contract(BaseModel):
    model_config = ConfigDict(extra='forbid', allow_inf_nan=False)


class FeatureInfo(Contract):
    name: str
    type_name: str | None = None


class EquationState(Contract):
    # Counts are separate API readings; do not add them or infer state from EqnFolder.
    count: int = Field(ge=0, strict=True)
    disabled_count: int = Field(ge=0, strict=True)
    linked_to_file: StrictBool


class ModelContext(Contract):
    document_title: str | None = None
    path: str | None = None
    document_type: Literal['none', 'part', 'assembly', 'drawing', 'unknown'] = 'none'
    document_id: str | None = Field(default=None, max_length=128)
    update_stamp: int | None = None
    configuration: str | None = None
    equations: EquationState | None = None
    features: list[FeatureInfo] = Field(default_factory=list, max_length=5000)


class PlateInputs(Contract):
    width_mm: Dimension
    height_mm: Dimension
    thickness_mm: Dimension
    plane: Literal['first_reference_plane'] = 'first_reference_plane'


class ThicknessInputs(Contract):
    thickness_mm: Dimension
    feature_name: Literal['Mechra-Plate-Extrude'] = 'Mechra-Plate-Extrude'


class CadOperation(Contract):
    id: str = Field(min_length=1, max_length=128)
    kind: OperationKind
    intent: str = Field(min_length=1)
    inputs: dict[str, Any]
    depends_on: list[str] = Field(default_factory=list, max_length=0)

    @model_validator(mode='after')
    def check_inputs(self):
        cls = PlateInputs if self.kind == 'create_plate' else ThicknessInputs
        self.inputs = cls.model_validate(self.inputs).model_dump()
        return self


class CadPlan(Contract):
    version: Literal['0.2'] = '0.2'
    document_type: Literal['part'] = 'part'
    summary: str = ''
    requires_confirmation: Literal[True] = True
    operations: list[CadOperation] = Field(min_length=1, max_length=1)

    @field_validator('requires_confirmation', mode='before')
    @classmethod
    def require_boolean_review(cls, value):
        if value is not True:
            raise ValueError('explicit boolean true is required')
        return value


class SpecItem(Contract):
    id: str
    field: str
    value: float | None
    unit: str = 'mm'
    status: Literal['confirmed', 'assumed', 'missing']
    source: str | None
    confidence: float = Field(ge=0, le=1)
    required_for: list[str]
    evidence: list[str] = Field(default_factory=list)


class DesignSpec(Contract):
    version: Literal['0.1'] = '0.1'
    source: str = 'user_text'
    items: list[SpecItem]


class ChatRequest(Contract):
    message: str = Field(min_length=1, max_length=4000)
    session_id: str | None = Field(default=None, min_length=1, max_length=128)
    context: ModelContext = Field(default_factory=ModelContext)

    @field_validator('message')
    @classmethod
    def nonblank(cls, value):
        if not value.strip():
            raise ValueError('message must not be blank')
        return value


class ChatResponse(Contract):
    message: str
    action: Literal['none', 'execute_cad_plan'] = 'none'
    status: Literal['info', 'clarification_required', 'plan_ready'] = 'info'
    requires_confirmation: bool = False
    plan: CadPlan | None = None
    design_spec: DesignSpec | None = None


class CadVerificationRequest(Contract):
    operation: OperationKind
    rebuild_ok: StrictBool
    expected_width_mm: Dimension
    expected_height_mm: Dimension
    expected_thickness_mm: Dimension
    measured_width_mm: Positive
    measured_height_mm: Positive
    measured_thickness_mm: Positive
    expected_volume_mm3: Positive
    measured_volume_mm3: Positive
    feature_names: list[str] = Field(default_factory=list, max_length=100)


class CadVerificationResponse(Contract):
    passed: bool
    message: str
    checks: list[str] = Field(default_factory=list)
