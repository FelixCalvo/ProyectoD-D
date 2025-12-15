using Fungus;

namespace Fungus
{
    [CommandInfo(
        "Narrative",
        "Say With Journal",
        "Say localizado que guarda el diálogo en el diario de aventuras"
    )]
    public class SayWithJournal : Say
    {
        public override void OnEnter()
        {
            // Ejecuta TODO el Say normal (localización, UI, wait, etc.)
            base.OnEnter();

            if (AdventureJournal.Instance == null) return;

            // Nombre del hablante
            string speakerName = character != null
                ? character.NameText
                : "Narrador";

            // Texto traducido (ya procesado por Say.OnEnter())
            string finalText = translatedText;

            // Evitar guardar basura
            if (string.IsNullOrEmpty(finalText)) return;

            AdventureJournal.Instance.AddEntry(speakerName, finalText);
        }
    }
}
