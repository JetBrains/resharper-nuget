<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                exclude-result-prefixes="msxsl">

  <xsl:output method="xml" indent="yes"/>

  <!-- This is the latest version available -->
  <xsl:variable name="LatestMajor" select="1" />
  <xsl:variable name="LatestMinor" select="2" />
  <xsl:variable name="LatestBuild" select="0" />

  <!-- Match the PluginLocalInfo element created by serialising the data from the category -->
  <xsl:template match="/PluginLocalInfo">
    <UpdateInfos>
      <xsl:variable name="InstalledMajor" select="PluginVersion/@Major" />
      <xsl:variable name="InstalledMinor" select="PluginVersion/@Minor" />
      <xsl:variable name="InstalledBuild" select="PluginVersion/@Build" />
    
      <!-- If we have a new version, add an <UpdateInfo /> element to tell ReSharper a new version is ready -->
      <xsl:if test="($InstalledMajor &lt; $LatestMajor) or ($InstalledMajor = $LatestMajor and $InstalledMinor &lt; $LatestMinor) or ($InstalledMajor = $LatestMajor and $InstalledMinor = $LatestMinor and $InstalledBuild &lt; $LatestBuild)">
        
        <UpdateInfo>
          <InformationUri>https://github.com/JetBrains/resharper-nuget/wiki/Release-Notes</InformationUri>
          <Title>
            <xsl:value-of select="concat('NuGet Support for ReSharper ', $LatestMajor, '.', $LatestMinor, '.', $LatestBuild, ' Released')" />
          </Title>
          <Description>A minor upgrade is available.</Description>
          <DownloadUri>
            <xsl:value-of select="concat('http://download.jetbrains.com/resharper/plugins/resharper-nuget.', $LatestMajor, '.', $LatestMinor, '.zip')" />
          </DownloadUri>
          <CompanyName>JetBrains</CompanyName>
          <ProductName>NuGet Support</ProductName>
          <ProductVersion><xsl:value-of select="concat($LatestMajor, '.', $LatestMinor, '.', $LatestBuild, '.0')"/></ProductVersion>
          <PriceTag />
          <IsFree>true</IsFree>
          <IconData>
              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
          </IconData>
        </UpdateInfo>
      
      </xsl:if>
    </UpdateInfos>
  </xsl:template>

</xsl:stylesheet>
