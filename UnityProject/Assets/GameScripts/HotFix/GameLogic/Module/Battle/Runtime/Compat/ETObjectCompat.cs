namespace ET
{
    // Battle legacy config types still inherit from ET.Object after migration.
    // Keep this base class intentionally empty so the runtime data model can compile
    // without reintroducing the old ET object lifecycle.
    public class Object
    {
    }
}
