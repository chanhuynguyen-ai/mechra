from typing import List, Optional
from pydantic import BaseModel, Field


class FeatureInfo(BaseModel):
    name: str
    type_name: Optional[str] = None


class ModelContext(BaseModel):
    document_title: Optional[str] = None
    path: Optional[str] = None
    document_type: str = "none"
    features: List[FeatureInfo] = Field(default_factory=list)


class ChatRequest(BaseModel):
    message: str
    context: ModelContext = Field(default_factory=ModelContext)


class ChatResponse(BaseModel):
    message: str
    action: str = "none"
    requires_confirmation: bool = False
