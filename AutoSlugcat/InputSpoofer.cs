using System.Collections.Generic;

namespace SlugBrain
{
    class InputSpoofer
    {
        public InputSpoofer()
        {
            ready = false;
        }

        public void ModifyInputs(ref Player.InputPackage originalInputs)
        {
            // TODO : make this more sofisticated and less override-y
            if (ready)
            {
                originalInputs = inputPackage;
            }
        }

        public void SetNewInputs(Player.InputPackage newInputs)
        {
            inputPackage = newInputs;
            ready = true;
        }

        bool ready;
        Player.InputPackage inputPackage;

    }
}
