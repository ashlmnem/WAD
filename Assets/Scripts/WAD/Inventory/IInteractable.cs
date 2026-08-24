namespace WAD.Inventory
{
    /// <summary>
    /// Gemeinsames Interface fuer alles, mit dem der Spieler per 'F' interagieren
    /// kann (Loot, Magazine, Leichen, Stationen, NPCs, lose Waffen).
    /// </summary>
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        void Interact(PlayerInteraction interactor);
    }
}