<xsl:stylesheet
    version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
    xmlns="http://schemas.microsoft.com/wix/2006/wi">
    <!-- identity match, copy all harvested files and filter afterward -->
    <xsl:template match="@*|*">
        <xsl:copy>
          <xsl:apply-templates select="@*" />
          <xsl:apply-templates select="*" />
        </xsl:copy>
      </xsl:template>
      <xsl:output method="xml" indent="yes" />

      <!-- Match to exactly the .exe name so that things like .exe.config will not be filtered -->
      <xsl:key name="search-exe" match="wix:Component['PISmartcardClient.exe' = substring(wix:File/@Source, string-length(wix:File/@Source) - string-length('PISmartcardClient.exe') + 1)]" use="@Id"/>
      <xsl:template match="wix:Component[key('search-exe', @Id)]" />
      <xsl:template match="wix:ComponentRef[key('search-exe', @Id)]" />

      <!-- Filter .pdb too -->
      <xsl:key name="search-pdb" match="wix:Component[substring(wix:File/@Source, string-length(wix:File/@Source) - string-length('.pdb') + 1)='.pdb']" use="@Id"/>
      <xsl:template match="wix:Component[key('search-pdb', @Id)]" />
      <xsl:template match="wix:ComponentRef[key('search-pdb', @Id)]" />
</xsl:stylesheet>