using System;
using System.Collections.Generic;
using System.Text;

namespace editor
{
    public class DocumentHistory
    {
        private List<TextState> states = new List<TextState>();
        private int currentIndex = -1;
        private int maxHistorySize = 100;

        public bool CanUndo = false;
        public bool CanRedo = false;

        public void AddState(TextState state)
        {
            if (currentIndex < states.Count - 1)
            {
                states.RemoveRange(currentIndex + 1, states.Count - currentIndex - 1);
            }

            states.Add(state);
            currentIndex = currentIndex + 1;

            if (currentIndex > 0)
                CanUndo = true;
            else
                CanUndo = false;
            if (currentIndex < states.Count - 1)
                CanRedo = true;
            else
                CanRedo = false;

            if (states.Count > maxHistorySize)
            {
                states.RemoveAt(0);
                currentIndex--;
            }
        }

        public TextState Undo()
        {
            if (!CanUndo) return null;

            currentIndex--;

            if (currentIndex > 0)
                CanUndo = true;
            else
                CanUndo = false;
            if (currentIndex < states.Count - 1)
                CanRedo = true;
            else
                CanRedo = false;

            return states[currentIndex];
        }

        public TextState Redo()
        {
            if (!CanRedo) return null;

            currentIndex++;

            if (currentIndex > 0)
                CanUndo = true;
            else
                CanUndo = false;
            if (currentIndex < states.Count - 1)
                CanRedo = true;
            else
                CanRedo = false;

            return states[currentIndex];
        }

        public TextState UndoAll()
        {
            if (states.Count == 0) return null;

            currentIndex = 0;

            if (currentIndex > 0)
                CanUndo = true;
            else
                CanUndo = false;
            if (currentIndex < states.Count - 1)
                CanRedo = true;
            else
                CanRedo = false;

            return states[0];
        }

        public void Clear()
        {
            states.Clear();
            currentIndex = -1;
        }

        public TextState GetCurrentState()
        {
            if (currentIndex >= 0 && currentIndex < states.Count)
                return states[currentIndex];
            return null;
        }
    }
}
