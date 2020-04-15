using Stateless;

namespace NosAyudamos
{
    public interface IDonor
    {
        State State { get; set; }
        void Register();
    }

    public class Donor : IDonor
    {
        private readonly StateMachine<State, Trigger> _machine;

        public State State { get; set; }

        public Donor()
        {
            _machine = new StateMachine<State, Trigger>(() => State, s => State = s);
            InitializeStateMachine();
        }

        public void Register()
        {
            _machine.Fire(Trigger.Register);
        }

        private void InitializeStateMachine()
        {
            _machine.Configure(State.New)
                .Permit(Trigger.Register, State.Registered);
        }
    }

    public enum State { New, Registered};
    public enum Trigger { Register, Registered, };
}