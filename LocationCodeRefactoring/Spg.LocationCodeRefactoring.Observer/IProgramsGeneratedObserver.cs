﻿namespace Spg.LocationCodeRefactoring.Observer
{
    public interface IProgramsGeneratedObserver
    {
        /// <summary>
        /// Notify program generated
        /// </summary>
        /// <param name="pEvent">Event</param>
        void NotifyProgramGenerated(ProgramGeneratedEvent pEvent);
    }
}
