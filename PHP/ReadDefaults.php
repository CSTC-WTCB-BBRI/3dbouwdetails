<?php

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

// Create the table
$tableCreation = "CREATE TABLE IF NOT EXISTS default_materials ( material_name VARCHAR(50) NOT NULL, surface_type VARCHAR(50) NOT NULL, in_out VARCHAR(5) NOT NULL, tiled TINYINT NOT NULL, PRIMARY KEY (surface_type, in_out, tiled)) CHARACTER SET 'utf8' ENGINE=INNODB;";
$result = $bdd->query($tableCreation);

if ($result->errorCode() == 00000) 
{
  echo "Default materials table creation: OK\r\n";
} 
else 
{
  echo "Error while creating the default materials table!\r\n";
}
$result->closeCursor();

//Insert comment
$insertCmd = "REPLACE INTO default_materials VALUES ('brick_4', 'mur', 'out', '0'), ('Ornamental_Tiles_basecolor1', 'mur','in', '1'), ('paintedPlaster', 'mur', 'in', '0'), ('woodFloor', 'sol', 'in', '0'), ('tiledFloor', 'sol', 'in', '1');";
$result = $bdd->query($insertCmd);

if ($result->errorCode() == 00000) 
{
  echo "Default materials table insertion: OK\r\n";
} 
else 
{
  echo "Error while inserting the default materials table!\r\n";
}
$result->closeCursor();

// Retrieve default materials
$getCmd = "SELECT * FROM default_materials ;";
$result = $bdd->query($getCmd);

if ($result->rowCount() > 0) {
  $rows = array();
	echo "Default material obtention: OK\r\n";
  echo "RETURNS;\r\n";
}
else 
{
  echo "Error while obtaining default materials!\r\n";
}

// output data of each row
while($row = $result->fetch(PDO::FETCH_ASSOC)) {
    $rows[] = $row;
} 
echo json_encode($rows);
$result->closeCursor();
?>