using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMF
{
	//This character movement input class is an example of how to get input from a keyboard to control the character;
    public class CharacterKeyboardInput : CharacterInput
    {
        private PlayerInputManager _inputManager;

		Vector2 movementInput;
        bool jumpInput;

        private void Awake()
        {
            _inputManager = GetComponent<PlayerInputManager>();

            _inputManager.OnInputReceived += OnInputReceived;
        }

        private void OnInputReceived(ButtonType button, InputType inputType, Vector2? inputAxis)
		{
			switch (button)
			{
                case ButtonType.LStick:
                    {
                        if (inputType == InputType.Hold)
                            movementInput = (Vector2)inputAxis;
                        else if (inputType == InputType.Release)
                            movementInput = Vector3.zero;
                    }                    
                    break;
                case ButtonType.South:
                    {
                        if (inputType == InputType.Press)
                            jumpInput = true;
                        else if (inputType == InputType.Release)
                            jumpInput = false;
                    }
                    break;
            }
		}

        public override float GetHorizontalMovementInput()
		{
            return movementInput.x;
        }

		public override float GetVerticalMovementInput()
		{
            return movementInput.y;
        }

		public override bool IsJumpKeyPressed()
		{
			return jumpInput;
		}
    }
}
