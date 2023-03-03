<?php

$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];
$surfaceID = $_POST["surfaceID"];

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

// Get comment
$getCmd = "SELECT comment, session FROM `c" . $clientId . "_p" . $projectId . "_comments` WHERE id_surface='" . $surfaceID . "' ORDER BY session DESC;";
$result = $bdd->query($getCmd);

if ($result->errorCode() == 00000) 
{
  echo "Comment obtention: OK\r\n";
} 
else 
{
  echo "Error while retrieving the comment!\r\n";
}

if ($result->rowCount() > 0) {
    echo "RETURNS\r\n";
}

// output data of each row
while($row = $result->fetch(PDO::FETCH_ASSOC)) {
    echo ";" . $row['comment'];
} 

//Close the query access
$result->closeCursor();


?>