namespace AgentApp.Hardware;

public interface IHardwareInfoProvider
{
    HardwareInfo Collect();
}