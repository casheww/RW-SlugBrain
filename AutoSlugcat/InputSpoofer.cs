using UnityEngine;

namespace SlugBrain
{
    class InputSpoofer
    {
        public InputSpoofer()
        {
            ready = false;
        }

        public void ModifyInputs(ref Player.InputPackage orig)
        {
            if (!ready) return;

            orig.x = Mathf.Clamp(orig.x + inputPackage.x, -1, 1);
            orig.y = Mathf.Clamp(orig.y + inputPackage.y, -1, 1);

            orig.jmp = orig.jmp || inputPackage.jmp;
            orig.mp = orig.mp || inputPackage.mp;
            orig.pckp = orig.pckp || inputPackage.pckp;
            orig.thrw = orig.thrw || inputPackage.thrw;

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
