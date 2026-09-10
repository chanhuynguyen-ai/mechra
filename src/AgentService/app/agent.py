from .models import ChatRequest, ChatResponse


class FoundationAgent:
    """Provider-neutral placeholder.

    v0.1 intentionally cannot mutate CAD. It proves the product boundary and
    context path first. v0.2 will add provider adapters + validated CAD plans.
    """

    def reply(self, request: ChatRequest) -> ChatResponse:
        ctx = request.context
        if ctx.document_type == "none":
            context_note = "No active SOLIDWORKS document is currently visible to the agent."
        else:
            context_note = (
                f"I can see {ctx.document_type} '{ctx.document_title}' with "
                f"{len(ctx.features)} top-level features."
            )
        return ChatResponse(
            message=(
                f"{context_note}\n\n"
                "Mechra v0.1.0 is connected. CAD mutation is intentionally disabled until "
                "the deterministic plan/execute/verify transaction is introduced in v0.2."
            )
        )
