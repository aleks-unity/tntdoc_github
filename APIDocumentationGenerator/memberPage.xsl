<?xml version='1.0'?>
<xsl:stylesheet 
	version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
>

<xsl:param name='XMLSourceDir' select='""' />

<xsl:template match='/'><xsl:apply-templates select='Model' /></xsl:template>


<xsl:template match='Model'>
<html lang="en">
<head>
	<meta http-equiv="content-type" content="text/html; charset=utf-8" />
	<meta http-equiv='content-style-type' content='text/css' />
	<link rel='stylesheet' href='../StaticFiles/docStyle.css' type='text/css' />
	<script src='../StaticFiles/jQuery.js' type='text/javascript'></script>
	<script src='../StaticFiles/SelectCodeSample.js' type='text/javascript'></script>
	<title>Untitled</title>
</head>
<body>

<div class='delta2'>
<span class='heading'><xsl:value-of select='Section/Signature/Declaration/@name' /></span>
<form id='langSelect'>
	<label for='langMenu'>Examples in </label>
	<select name='langMenu' id='langMenu' onchange='languageChanged(document.getElementById("langMenu").selectedIndex)'>
		<option>JavaScript</option>
		<option>C#</option>
		<option>Boo</option>
	</select>
</form>

<xsl:apply-templates select='Section' />

<xsl:apply-templates select='Children' />

</div>
</body>
</html>
</xsl:template>


<xsl:template match='Section'>
<div class='section'>

<div class='subsection'>
<div class='sigBlock'>
<xsl:apply-templates select='Signature' />
</div>
</div>

<xsl:if test='ParamWithDoc'>
<div class='subsection'>
<span class='subheading'>Parameters</span><br/>
<table class='paramTable'>
<xsl:apply-templates select='ParamWithDoc' />
</table>
</div>
</xsl:if>

<xsl:if test='ReturnWithDoc'>
<div class='subsection'>
<span class='subheading'>Returns</span><br/>
<xsl:apply-templates select='ReturnWithDoc' />
</div>
</xsl:if>


<div class='subsection'>
<span class='subheading'>Description</span><br/>
<xsl:apply-templates select='Summary' />
</div>

<xsl:if test='Example'>
<div class='subsection'>
<span class='subheading'>Example</span><br/>
<xsl:apply-templates select='Example' />
</div>
</xsl:if>

</div>
</xsl:template>


<xsl:template match='ParamWithDoc'>
<tr><td class='paramName'><xsl:value-of select='name' /></td><td><xsl:value-of select='doc' /></td></tr>
</xsl:template>


<xsl:template match='ReturnWithDoc'>
<xsl:value-of select='@type' /> - <xsl:value-of select='.' />
</xsl:template>


<xsl:template match='Signature'>
<div class='sigBlockJS'>
<span class='decText'><xsl:if test='Declaration/@type="function"'>
<xsl:if test='Declaration/@modifiers'><xsl:value-of select='Declaration/@modifiers' /></xsl:if>
function
<xsl:value-of select='Declaration/@name' />(<xsl:apply-templates select='ParamElement' mode='JS' />)<xsl:if test='ReturnType'>:
<xsl:value-of select='ReturnType' />;
</xsl:if>
</xsl:if>
<xsl:if test='Declaration/@type="var"'>
var <xsl:value-of select='Declaration/@name' />: <xsl:value-of select='ReturnType' />;
</xsl:if>
</span>
</div>

<div class='sigBlockCS'>
<span class='decText'><xsl:if test='Declaration/@type="function"'>
<xsl:if test='Declaration/@modifiers'><xsl:value-of select='concat(Declaration/@modifiers, " ")' /></xsl:if>

<xsl:value-of select='concat(ReturnType, " ")' />
<xsl:value-of select='Declaration/@name' />(<xsl:apply-templates select='ParamElement' mode='CS' />);</xsl:if>
<xsl:if test='Declaration/@type="var"'>
<xsl:value-of select='ReturnType' /><xsl:text> </xsl:text>
<xsl:value-of select='Declaration/@name' />;
</xsl:if>
</span>
</div>

<div class='sigBlockBoo'>
<span class='decText'><xsl:if test='Declaration/@type="function"'>
<xsl:if test='Declaration/@modifiers'><xsl:value-of select='Declaration/@modifiers' /></xsl:if>
def
<xsl:value-of select='Declaration/@name' />(<xsl:apply-templates select='ParamElement' mode='Boo' />)<xsl:if test='ReturnType'>
as <xsl:value-of select='ReturnType' />
</xsl:if>
</xsl:if>
<xsl:if test='Declaration/@type="var"'>
<xsl:value-of select='Declaration/@name' /> as <xsl:value-of select='ReturnType' />
</xsl:if>
</span>
</div>
</xsl:template>


<xsl:template match='ParamElement[1]' mode='JS'><xsl:if test='@modifier'><xsl:value-of select='concat(@modifier, " ")' />
 </xsl:if><xsl:value-of select='@name' />:
<xsl:value-of select='@type' /></xsl:template>

<xsl:template match='ParamElement' mode='JS'>,
<xsl:if test='@modifier'><xsl:value-of select='concat(@modifier, " ")' /></xsl:if>
<xsl:value-of select='@name' />: <xsl:value-of select='@type' /></xsl:template>


<xsl:template match='ParamElement[1]' mode='CS'><xsl:if test='@modifier'><xsl:value-of select='concat(@modifier, " ")' />
 </xsl:if><xsl:value-of select='concat(@type, " ")' /> <xsl:value-of select='@name' />
</xsl:template>

<xsl:template match='ParamElement' mode='CS'>,
<xsl:if test='@modifier'><xsl:value-of select='concat(@modifier, " ")' /></xsl:if>
<xsl:value-of select='concat(@type, " ")' /> <xsl:value-of select='@name' /></xsl:template>


<xsl:template match='ParamElement[1]' mode='Boo'><xsl:choose>
<xsl:when test='@modifier and @modifier="params"'>*</xsl:when>
<xsl:when test='@modifier'><xsl:value-of select='concat(@modifier, " ")' /></xsl:when>
</xsl:choose>
<xsl:value-of select='@name' /> as <xsl:value-of select='@type' />
</xsl:template>

<xsl:template match='ParamElement' mode='Boo'>,
<xsl:choose>
<xsl:when test='@modifier and @modifier="params"'>*</xsl:when>
<xsl:otherwise><xsl:value-of select='concat(@modifier, " ")' /></xsl:otherwise>
</xsl:choose>
<xsl:value-of select='@name' /> as <xsl:value-of select='@type' /> </xsl:template>



<xsl:template match='Summary'>
<xsl:apply-templates />
</xsl:template>


<xsl:template match='Example/JavaScript'>
<pre class='codeExampleJS'><xsl:apply-templates /></pre>
</xsl:template>

<xsl:template match='Example/CSharp'>
<pre class='codeExampleCS'><xsl:apply-templates /></pre>
</xsl:template>

<xsl:template match='Example/Boo'>
<pre class='codeExampleBoo'><xsl:apply-templates /></pre>
</xsl:template>


<xsl:template match='Children'>
<span class='subheading'>Members</span>
<table class='memberTable'>
<xsl:apply-templates select='member_id' />
</table>
</xsl:template>

<xsl:template match='member_id'>
<xsl:variable name='childDetails' select='document(concat($XMLSourceDir, "/", ., ".xml"))/Model/Section' />
<tr>
<th class='memberTableName'>
<a href='{.}.html'>
<xsl:value-of select='$childDetails/Signature/Declaration/@name' />
</a>
</th>
<td><xsl:value-of select='$childDetails/Summary' /></td>
</tr>
</xsl:template>


</xsl:stylesheet>
