%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe DataExchangeService.exe
Net Start DataExchangeService
sc config DataExchangeService start= auto
pause