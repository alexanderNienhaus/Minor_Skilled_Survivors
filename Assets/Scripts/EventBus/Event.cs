using Unity.Collections;
using Unity.Entities;

public class CustomEvent
{
}

public class OnSelectedPlacableObjectChanged : CustomEvent
{

    public OnSelectedPlacableObjectChanged()
    {
    }
}

public class OnMoveCommandIssued : CustomEvent
{
    public NativeArray<Entity> units;

    public OnMoveCommandIssued(NativeArray<Entity> pUnits)
    {
        units = pUnits;
    }
}

public class OnSelectedUnitCountChangeEvent : CustomEvent
{
    public int selectedUnitCount;

    public OnSelectedUnitCountChangeEvent(int pUnitCount)
    {
        selectedUnitCount = pUnitCount;
    }
}
