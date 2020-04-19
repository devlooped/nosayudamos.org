using Stateless;

namespace NosAyudamos
{
    interface IDonor
    {
        State State { get; set; }
        void Register();
    }

    class Donor : IDonor
    {
        private readonly StateMachine<State, Trigger> machine;

        public State State { get; set; }

        public Donor()
        {
            machine = new StateMachine<State, Trigger>(() => State, s => State = s);
            InitializeStateMachine();
        }

        public void Register()
        {
            machine.Fire(Trigger.Register);
        }

        private void InitializeStateMachine()
        {
            machine.Configure(State.New)
                .Permit(Trigger.Register, State.Registered);
        }
    }

    enum State { New, Registered };
    enum Trigger { Register, Registered, };
}
