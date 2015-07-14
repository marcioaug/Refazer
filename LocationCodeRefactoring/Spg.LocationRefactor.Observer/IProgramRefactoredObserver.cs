namespace Spg.LocationRefactor.Observer
{
    public interface IProgramRefactoredObserver
    {
        /// <summary>
        /// Notify program refactored
        /// </summary>
        /// <param name="pEvent">Event</param>
        void NotifyProgramRefactored(ProgramRefactoredEvent pEvent);
    }
}
